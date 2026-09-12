using System.Text;

namespace MouseController;

/// <summary>一组修饰键 + 主键的绑定。Vk = 0 表示"方向键"（仅移动类使用，方向由实际按下的方向键决定）。</summary>
public sealed class KeyBinding
{
    public bool Ctrl { get; set; }
    public bool Shift { get; set; }
    public bool Alt { get; set; }
    public bool Win { get; set; }
    public uint Vk { get; set; }

    /// <summary>修饰键数量。</summary>
    public int ModCount => (Ctrl ? 1 : 0) + (Shift ? 1 : 0) + (Alt ? 1 : 0) + (Win ? 1 : 0);

    /// <summary>与一次实际按键是否吻合（方向类绑定忽略主键）。</summary>
    public bool Matches(HotkeyInfo h) =>
        Ctrl == h.Ctrl && Shift == h.Shift && Alt == h.Alt && Win == h.Win &&
        (Vk == 0 || Vk == h.Vk);

    /// <summary>两个绑定是否互相冲突（方向类绑定只比较修饰键）。</summary>
    public bool ConflictsWith(KeyBinding other) =>
        Ctrl == other.Ctrl && Shift == other.Shift && Alt == other.Alt && Win == other.Win &&
        (Vk == 0 || other.Vk == 0 || Vk == other.Vk);

    /// <summary>显示文本，如 "Ctrl+Alt+1"、"Alt+方向键"。</summary>
    public string Display(bool directional)
    {
        var sb = new StringBuilder();
        if (Ctrl) sb.Append("Ctrl+");
        if (Shift) sb.Append("Shift+");
        if (Alt) sb.Append("Alt+");
        if (Win) sb.Append("Win+");
        sb.Append(directional ? "方向键" : VkNames.Of(Vk));
        return sb.ToString();
    }

    public KeyBinding Clone() => new() { Ctrl = Ctrl, Shift = Shift, Alt = Alt, Win = Win, Vk = Vk };
}

/// <summary>全部可自定义的快捷键。</summary>
public sealed class HotkeySettings
{
    public KeyBinding MoveLarge { get; set; } = new() { Alt = true };
    public KeyBinding MoveMedium { get; set; } = new() { Alt = true, Shift = true };
    public KeyBinding MoveSmall { get; set; } = new() { Alt = true, Ctrl = true };
    public KeyBinding MoveTiny { get; set; } = new() { Alt = true, Ctrl = true, Shift = true };

    public KeyBinding ClickLeft { get; set; } = new() { Ctrl = true, Alt = true, Vk = 0x31 };
    public KeyBinding ClickMiddle { get; set; } = new() { Ctrl = true, Alt = true, Vk = 0x32 };
    public KeyBinding ClickRight { get; set; } = new() { Ctrl = true, Alt = true, Vk = 0x33 };
    public KeyBinding ClickerToggle { get; set; } = new() { Ctrl = true, Alt = true, Vk = 0x34 };
    public KeyBinding ShowMain { get; set; } = new() { Ctrl = true, Vk = 0x4D };
    public KeyBinding Quit { get; set; } = new() { Ctrl = true, Alt = true, Vk = 0x51 };

    public HotkeySettings Clone() => new()
    {
        MoveLarge = MoveLarge.Clone(),
        MoveMedium = MoveMedium.Clone(),
        MoveSmall = MoveSmall.Clone(),
        MoveTiny = MoveTiny.Clone(),
        ClickLeft = ClickLeft.Clone(),
        ClickMiddle = ClickMiddle.Clone(),
        ClickRight = ClickRight.Clone(),
        ClickerToggle = ClickerToggle.Clone(),
        ShowMain = ShowMain.Clone(),
        Quit = Quit.Clone(),
    };
}

/// <summary>虚拟键码 → 可读名称。</summary>
public static class VkNames
{
    private static readonly Dictionary<uint, string> Map = new()
    {
        [0x08] = "Backspace", [0x09] = "Tab", [0x0D] = "Enter", [0x1B] = "Esc",
        [0x20] = "Space", [0x21] = "PgUp", [0x22] = "PgDn", [0x23] = "End", [0x24] = "Home",
        [0x25] = "方向键", [0x26] = "方向键", [0x27] = "方向键", [0x28] = "方向键",
        [0x2D] = "Insert", [0x2E] = "Delete",
        [0x5B] = "Win", [0x5C] = "Win",
        [0xBA] = ";", [0xBB] = "=", [0xBC] = ",", [0xBD] = "-", [0xBE] = ".", [0xBF] = "/",
        [0xC0] = "`", [0xDB] = "[", [0xDC] = "\\", [0xDD] = "]", [0xDE] = "'",
    };

    static VkNames()
    {
        for (uint i = 0; i <= 9; i++) Map[0x30 + i] = i.ToString();
        for (uint i = 0; i < 26; i++) Map[0x41 + i] = ((char)('A' + i)).ToString();
        for (uint i = 0; i < 12; i++) Map[0x70 + i] = "F" + (i + 1);
    }

    public static string Of(uint vk) => Map.TryGetValue(vk, out var s) ? s : $"0x{vk:X2}";
}

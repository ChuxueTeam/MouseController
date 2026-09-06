using System.Runtime.InteropServices;

namespace MouseController;

/// <summary>Win32 原生 API 封装：鼠标位置、鼠标模拟。</summary>
internal static class Native
{
    // ---- 虚拟键码 ----
    public const uint VK_UP = 0x26;
    public const uint VK_DOWN = 0x28;
    public const uint VK_LEFT = 0x25;
    public const uint VK_RIGHT = 0x27;
    public const uint VK_SPACE = 0x20;
    public const uint VK_M = 0x4D;
    public const uint VK_Q = 0x51;
    public const uint VK_1 = 0x31;
    public const uint VK_2 = 0x32;
    public const uint VK_3 = 0x33;
    public const uint VK_4 = 0x34;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    // ---- 单实例唤醒 ----------------
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    /// <summary>"再次运行→显示主界面"消息。与窗口标题常量同源，只在本程序内有效。</summary>
    public static readonly uint WM_SHOWMAIN = RegisterWindowMessage("MouseController.ShowMain.77F3A2E1");

    /// <summary>找到已运行实例的主窗口并告诉它显示主界面（隐藏中也能收到）。</summary>
    public static void NudgeExistingInstance()
    {
        IntPtr h = FindWindow(null, "MouseController · 鼠标控制器");
        if (h != IntPtr.Zero)
            PostMessage(h, WM_SHOWMAIN, IntPtr.Zero, IntPtr.Zero);
    }

    // ---- SendInput 模拟鼠标 ----
    public const int INPUT_MOUSE = 0;

    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public int type;
        public MOUSEINPUT mi;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    /// <summary>在当前光标位置执行一次完整点击（按下+抬起）。</summary>
    public static void ClickMouse(uint downFlag, uint upFlag)
    {
        var inputs = new INPUT[2];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].mi.dwFlags = downFlag;
        inputs[1].type = INPUT_MOUSE;
        inputs[1].mi.dwFlags = upFlag;
        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }
}

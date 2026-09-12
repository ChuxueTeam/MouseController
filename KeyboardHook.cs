using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MouseController;

/// <summary>组合键信息。</summary>
public readonly record struct HotkeyInfo(uint Vk, bool Ctrl, bool Shift, bool Win, bool Alt)
{
    /// <summary>按下的修饰键数量。</summary>
    public int ModCount => (Ctrl ? 1 : 0) + (Shift ? 1 : 0) + (Win ? 1 : 0) + (Alt ? 1 : 0);
}

/// <summary>
/// 低级键盘钩子（WH_KEYBOARD_LL）。
/// 系统级占用的组合键（Win+方向=snap、Ctrl+Win+左/右=虚拟桌面、Win+Space=输入法切换、Win+Q 等）
/// 无法用 RegisterHotKey 注册，用钩子在各程序之前取得按键：ComboDown 返回 true 即消费该键，
/// 不会传给别人，也阻止系统手势触发。只挂接主线程（安装线程），回调在 UI 线程消息循环内执行。
/// </summary>
public sealed class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const uint LLKHF_INJECTED = 0x10;

    private const int VK_LCONTROL = 0xA2;
    private const int VK_RCONTROL = 0xA3;
    private const int VK_LSHIFT = 0xA0;
    private const int VK_RSHIFT = 0xA1;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;
    private const int VK_MENU = 0x12;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookExW(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandleW(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private readonly LowLevelKeyboardProc _proc;
    private readonly HashSet<uint> _swallowedDown = new();
    private IntPtr _hookId;

    /// <summary>组合键按下事件。返回 true 表示消费该按键（不传给系统/其它程序）。</summary>
    public event Func<HotkeyInfo, bool>? ComboDown;

    public KeyboardHook()
    {
        _proc = HookProc;
    }

    public bool Install()
    {
        if (_hookId != IntPtr.Zero) return true;
        _hookId = SetWindowsHookExW(WH_KEYBOARD_LL, _proc, GetModuleHandleW(null), 0);
        return _hookId != IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg is WM_KEYDOWN or WM_SYSKEYDOWN or WM_KEYUP or WM_SYSKEYUP)
                {
                    var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    if ((data.flags & LLKHF_INJECTED) == 0)
                    {
                        bool isDown = msg is WM_KEYDOWN or WM_SYSKEYDOWN;
                        if (isDown)
                        {
                            var info = ReadModifiers(data.vkCode);
                            bool consumed = ComboDown?.Invoke(info) ?? false;
                            if (consumed)
                                _swallowedDown.Add(data.vkCode); // 记住，keyup 也吞掉
                        }
                        else if (_swallowedDown.Remove(data.vkCode))
                        {
                            return new IntPtr(1);
                        }
                    }
                }
            }
        }
        catch
        {
            // 钩子回调内绝不允许异常穿越到非托管层
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private static HotkeyInfo ReadModifiers(uint vk)
    {
        bool ctrl = IsDown(VK_LCONTROL, VK_RCONTROL);
        bool shift = IsDown(VK_LSHIFT, VK_RSHIFT);
        bool win = IsDown(VK_LWIN, VK_RWIN);
        bool alt = IsDown(VK_MENU, VK_MENU);
        return new HotkeyInfo(vk, ctrl, shift, win, alt);
    }

    private static bool IsDown(int l, int r) => ((GetAsyncKeyState(l) | GetAsyncKeyState(r)) & 0x8000) != 0;
}

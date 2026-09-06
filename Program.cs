using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace MouseController;

internal static class Program
{
    private const string MutexName = "MouseController_77F3A2E1-93C4-4A6B-8D2E-4C2F0A1B9E5D";

    private static Mutex? _mutex;

    [STAThread]
    private static void Main(string[] args)
    {
        // 自检模式：检查键盘钩子能否安装，不启动界面
        if (args.Length > 0 && args[0] == "--selftest")
        {
            Selftest.Run();
            return;
        }

        // 单实例保护：已在运行则不弹对话框，直接唤出已有实例的主界面
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            Native.NudgeExistingInstance();
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(AppSettings.Load()));
        GC.KeepAlive(_mutex);
    }
}

/// <summary>
/// --selftest：安装键盘钩子并输出结果。
/// 钩子方案下组合键不依赖 RegisterHotKey，不存在系统占用冲突；
/// 此模式用于排查杀毒软件/系统策略是否拦截钩子安装。
/// </summary>
internal static class Selftest
{
    public static void Run()
    {
        using var hook = new KeyboardHook();
        bool ok = hook.Install();
        Console.WriteLine(ok
            ? "OK   WH_KEYBOARD_LL hook installed"
            : $"FAIL WH_KEYBOARD_LL hook install (err={Marshal.GetLastWin32Error()})");
        Console.WriteLine("--selftest done--");
    }
}

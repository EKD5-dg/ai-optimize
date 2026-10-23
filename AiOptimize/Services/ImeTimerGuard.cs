using System.Runtime.InteropServices;
using System.Text;

namespace AiOptimize.Services;

/// <summary>
/// 搜狗拼音等 IME 会向进程注入 UI 窗口，并在 UI 线程上挂约 64Hz 的 WM_TIMER。
/// 本程序主窗口没有任何输入控件，这类定时器纯属空转（实测占 13-17% 单核）。
/// 这里安装线程级消息钩子：只对 IME UI 窗口的 WM_TIMER 做 KillTimer 并丢弃消息。
/// 打开启动项管理（含搜索框）时调用 <see cref="Suspend"/>，关闭后恢复，保证中文输入可用。
/// </summary>
internal static class ImeTimerGuard
{
    private const int WH_GETMESSAGE = 3;
    private const int WM_NULL = 0;
    private const int WM_TIMER = 0x0113;
    private static readonly string[] ImeUiClasses = { "SoPY_UI" };

    private static HookProc? _hook; // 必须保持引用：委托被回收后钩子会失效
    private static bool _suspended;
    private static bool _installed;

    public static void Install()
    {
        if (_installed) return;
        _hook = Hook;
        _installed = SetWindowsHookEx(WH_GETMESSAGE, _hook, IntPtr.Zero, GetCurrentThreadId()) != IntPtr.Zero;
    }

    public static void Suspend() => _suspended = true;

    public static void Resume() => _suspended = false;

    private static IntPtr Hook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && !_suspended)
        {
            var msg = Marshal.PtrToStructure<MSG>(lParam);
            if (msg.message == WM_TIMER && IsImeUiWindow(msg.hwnd))
            {
                KillTimer(msg.hwnd, msg.wParam);
                msg.message = WM_NULL;
                Marshal.StructureToPtr(msg, lParam, false);
            }
        }
        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    private static bool IsImeUiWindow(IntPtr hwnd)
    {
        var cls = new StringBuilder(64);
        GetClassName(hwnd, cls, cls.Capacity);
        var name = cls.ToString();
        foreach (var candidate in ImeUiClasses)
        {
            if (name.Equals(candidate, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool KillTimer(IntPtr hWnd, IntPtr uIDEvent);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}

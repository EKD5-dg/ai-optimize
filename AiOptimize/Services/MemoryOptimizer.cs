using System.Diagnostics;
using AiOptimize.Native;

namespace AiOptimize.Services;

public sealed record MemoryOptimizeResult(double BeforeUsage, double AfterUsage, long FreedBytes, int ProcessesTrimmed);

/// <summary>内存释放：压缩各进程工作集 + 清空系统待机内存列表。</summary>
public sealed class MemoryOptimizer
{
    public Task<MemoryOptimizeResult> OptimizeAsync() => Task.Run(() =>
    {
        var before = ReadMemory();
        int trimmed = 0;
        int selfId = Environment.ProcessId;

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.Id == selfId) continue;
                if (NativeMethods.EmptyWorkingSet(process.Handle)) trimmed++;
            }
            catch { /* 无权限或已退出的进程跳过 */ }
            finally { process.Dispose(); }
        }

        PurgeStandbyList();

        Thread.Sleep(500); // 等系统内存统计刷新
        var after = ReadMemory();
        long freed = Math.Max(0, (long)before.Used - (long)after.Used);
        return new MemoryOptimizeResult(before.Usage, after.Usage, freed, trimmed);
    });

    private static (ulong Used, double Usage) ReadMemory()
    {
        var mem = NativeMethods.MEMORYSTATUSEX.Create();
        if (!NativeMethods.GlobalMemoryStatusEx(ref mem)) return (0, 0);
        ulong used = mem.ullTotalPhys - mem.ullAvailPhys;
        return (used, used * 100.0 / mem.ullTotalPhys);
    }

    private static void PurgeStandbyList()
    {
        try
        {
            EnablePrivilege(NativeMethods.SE_PROFILE_SINGLE_PROCESS_NAME);
            int command = NativeMethods.MemoryPurgeStandbyList;
            NativeMethods.NtSetSystemInformation(NativeMethods.SystemMemoryListInformation, ref command, sizeof(int));
        }
        catch { /* 权限不足时静默降级 */ }
    }

    private static void EnablePrivilege(string privilege)
    {
        using var current = Process.GetCurrentProcess();
        if (!NativeMethods.OpenProcessToken(current.Handle,
                NativeMethods.TOKEN_ADJUST_PRIVILEGES | NativeMethods.TOKEN_QUERY, out var token))
            return;
        try
        {
            if (!NativeMethods.LookupPrivilegeValue(null, privilege, out var luid)) return;
            var tp = new NativeMethods.TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = NativeMethods.SE_PRIVILEGE_ENABLED,
            };
            NativeMethods.AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            NativeMethods.CloseHandle(token);
        }
    }
}

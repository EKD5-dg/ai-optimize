using System.Diagnostics;
using AiOptimize.Models;
using AiOptimize.Native;

namespace AiOptimize.Services;

public sealed record MemoryOptimizeResult(double BeforeUsage, double AfterUsage, long FreedBytes, int ProcessesTrimmed);

/// <summary>内存释放：压缩各进程工作集 + 清空系统待机内存列表。</summary>
public sealed class MemoryOptimizer
{
    /// <summary>扫描当前最占内存的进程（同名合并）。</summary>
    public Task<IReadOnlyList<ScanItem>> ScanDetailsAsync(int top = 8) => Task.Run<IReadOnlyList<ScanItem>>(() =>
    {
        var entries = new List<(string Name, long Bytes)>();
        foreach (var process in Process.GetProcesses())
        {
            try { entries.Add((process.ProcessName, process.WorkingSet64)); }
            catch { }
            finally { process.Dispose(); }
        }
        return SummarizeProcesses(entries, top);
    });

    /// <summary>按进程名合并内存占用，降序取前 top 项；多进程时名称标注 ×N。</summary>
    public static List<ScanItem> SummarizeProcesses(IEnumerable<(string Name, long Bytes)> processes, int top)
        => processes
            .GroupBy(p => p.Name)
            .Select(g => new ScanItem(g.Count() > 1 ? $"{g.Key} ×{g.Count()}" : g.Key, g.Sum(p => p.Bytes)))
            .OrderByDescending(item => item.Bytes)
            .Take(top)
            .ToList();

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
            if (!EnablePrivilege(NativeMethods.SE_PROFILE_SINGLE_PROCESS_NAME))
            {
                Debug.WriteLine("[MemoryOptimizer] 无法启用 SeProfileSingleProcessPrivilege，跳过待机列表清理");
                return;
            }
            int command = NativeMethods.MemoryPurgeStandbyList;
            int status = NativeMethods.NtSetSystemInformation(
                NativeMethods.SystemMemoryListInformation, ref command, sizeof(int));
            if (status != 0) // NTSTATUS：0 = STATUS_SUCCESS
                Debug.WriteLine($"[MemoryOptimizer] 待机列表清理失败，NTSTATUS=0x{status:X8}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MemoryOptimizer] 待机列表清理异常：{ex.Message}");
        }
    }

    /// <summary>启用指定特权。AdjustTokenPrivileges 返回 true 不代表特权已生效，必须再查 GetLastError。</summary>
    private static bool EnablePrivilege(string privilege)
    {
        using var current = Process.GetCurrentProcess();
        if (!NativeMethods.OpenProcessToken(current.Handle,
                NativeMethods.TOKEN_ADJUST_PRIVILEGES | NativeMethods.TOKEN_QUERY, out var token))
            return false;
        try
        {
            if (!NativeMethods.LookupPrivilegeValue(null, privilege, out var luid)) return false;
            var tp = new NativeMethods.TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = NativeMethods.SE_PRIVILEGE_ENABLED,
            };
            if (!NativeMethods.AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
                return false;
            // ERROR_NOT_ALL_ASSIGNED(1300) 表示特权未全部授予
            return System.Runtime.InteropServices.Marshal.GetLastWin32Error() == 0;
        }
        finally
        {
            NativeMethods.CloseHandle(token);
        }
    }
}

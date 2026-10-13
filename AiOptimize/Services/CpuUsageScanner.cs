using System.Diagnostics;
using AiOptimize.Models;

namespace AiOptimize.Services;

public sealed record CpuCoolResult(int Killed, int Demoted);

/// <summary>扫描当前 CPU 占用最高的进程（两次采样求每进程处理器时间增量），并提供手动结束与「一键降温」。</summary>
public sealed class CpuUsageScanner
{
    /// <summary>采样窗口毫秒数。太短测不准短促任务，太长展开后要等；800ms 与任务管理器的思路一致。</summary>
    private const int SampleWindowMs = 800;

    /// <summary>「一键降温」的处理阈值：占总处理器能力低于此值的后台进程不动。</summary>
    public const double CoolMinUsage = 50;

    /// <summary>系统关键进程：无论占用多高都绝不结束、不降优先级，否则会蓝屏或锁死系统。</summary>
    private static readonly HashSet<string> CriticalNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "system", "registry", "memory compression", "secure system",
        "csrss", "smss", "wininit", "winlogon", "services", "lsass", "lsaiso",
        "svchost", "dwm", "fontdrvhost", "audiodg", "mshidcorpdump", "wudfhost",
        "msmpeng", // Windows Defender：杀了会立刻重启，且可能放过真正威胁
    };

    /// <summary>扫描每个进程的 CPU 占用（含 PID 与是否有窗口），供明细合并与降温挑选共用。</summary>
    public Task<IReadOnlyList<CpuProcessSample>> ScanSamplesAsync()
        => Task.Run<IReadOnlyList<CpuProcessSample>>(ScanSamples);

    /// <summary>合并成明细列表展示：按进程名合并，降序取前 top 项；多进程时名称标注 ×N 并保留全部 PID。</summary>
    public async Task<IReadOnlyList<CpuProcessItem>> ScanDetailsAsync(int top = 8)
        => SummarizeProcesses(await ScanSamplesAsync(), top);

    /// <summary>按进程名合并 CPU 占用，降序取前 top 项；同名进程的 PID 全部保留。</summary>
    public static List<CpuProcessItem> SummarizeProcesses(IEnumerable<CpuProcessSample> samples, int top)
        => samples
            .GroupBy(p => p.Name)
            .Select(g => new CpuProcessItem(
                g.Count() > 1 ? $"{g.Key} ×{g.Count()}" : g.Key,
                g.Sum(p => p.Usage),
                g.Select(p => p.Pid).OrderBy(pid => pid).ToList()))
            .OrderByDescending(item => item.Usage)
            .Take(top)
            .ToList();

    /// <summary>「一键降温」挑选：无可见窗口且占用超阈值的后台进程 → 结束；有窗口的高占用程序 → 只降优先级不关闭。</summary>
    public static (List<CpuProcessSample> ToKill, List<CpuProcessSample> ToDemote) SelectCoolTargets(
        IEnumerable<CpuProcessSample> samples, int selfPid)
    {
        var toKill = new List<CpuProcessSample>();
        var toDemote = new List<CpuProcessSample>();
        foreach (var sample in samples)
        {
            if (sample.Usage < CoolMinUsage || sample.Pid == selfPid || CriticalNames.Contains(sample.Name)) continue;
            if (sample.HasWindow) toDemote.Add(sample);
            else toKill.Add(sample);
        }
        return (toKill, toDemote);
    }

    /// <summary>一键降温：结束选中的后台进程 + 给有窗口的高占用程序降优先级。</summary>
    public Task<CpuCoolResult> CoolAsync() => Task.Run(() =>
    {
        var (toKill, toDemote) = SelectCoolTargets(ScanSamples(), Environment.ProcessId);

        int killed = 0;
        foreach (var target in toKill)
            if (EndProcess(target.Pid)) killed++;

        int demoted = 0;
        foreach (var target in toDemote)
        {
            try
            {
                using var process = Process.GetProcessById(target.Pid);
                process.PriorityClass = ProcessPriorityClass.BelowNormal;
                demoted++;
            }
            catch { /* 已退出或无权限：跳过 */ }
        }
        return new CpuCoolResult(killed, demoted);
    });

    /// <summary>结束指定进程：先请求正常退出（给程序保存数据的机会），1 秒没退再强制结束。返回是否已结束。</summary>
    public static bool EndProcess(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            if (process.MainWindowHandle != 0)
            {
                process.CloseMainWindow(); // 走程序自己的退出流程，未保存数据有机会弹窗
                if (process.WaitForExit(1000)) return true;
            }
            process.Kill();
            process.WaitForExit(3000);
            return true;
        }
        catch { return false; /* 已退出或无权限 */ }
    }

    /// <summary>记录每个进程的 PID、名称、累计处理器时间（Tick）与是否有可见窗口。</summary>
    private static Dictionary<int, (string Name, long Ticks, bool HasWindow)> Sample()
    {
        var map = new Dictionary<int, (string, long, bool)>();
        foreach (var process in Process.GetProcesses())
        {
            try { map[process.Id] = (process.ProcessName, process.TotalProcessorTime.Ticks, process.MainWindowHandle != 0); }
            catch { /* 已退出或无权限的进程跳过 */ }
            finally { process.Dispose(); }
        }
        return map;
    }

    private static IReadOnlyList<CpuProcessSample> ScanSamples()
    {
        var stopwatch = Stopwatch.StartNew();
        var first = Sample();
        Thread.Sleep(SampleWindowMs);
        var second = Sample();
        stopwatch.Stop();

        long elapsedTicks = stopwatch.Elapsed.Ticks;
        if (elapsedTicks <= 0 || first.Count == 0) return Array.Empty<CpuProcessSample>();
        int cores = Environment.ProcessorCount;

        var samples = new List<CpuProcessSample>(second.Count);
        foreach (var (id, (name, endTicks, hasWindow)) in second)
        {
            if (!first.TryGetValue(id, out var start) || start.Name != name) continue;
            double usage = (endTicks - start.Ticks) * 100.0 / elapsedTicks / cores;
            if (usage > 0) samples.Add(new CpuProcessSample(id, name, usage, hasWindow));
        }
        return samples;
    }
}

using System.Diagnostics;
using AiOptimize.Models;

namespace AiOptimize.Services;

/// <summary>扫描当前 CPU 占用最高的进程：两次采样求每进程处理器时间增量，再按进程名合并。</summary>
public sealed class CpuUsageScanner
{
    /// <summary>采样窗口毫秒数。太短测不准短促任务，太长展开后要等；800ms 与任务管理器的思路一致。</summary>
    private const int SampleWindowMs = 800;

    public Task<IReadOnlyList<CpuProcessItem>> ScanDetailsAsync(int top = 8) => Task.Run<IReadOnlyList<CpuProcessItem>>(() =>
    {
        var stopwatch = Stopwatch.StartNew();
        var first = Sample();
        Thread.Sleep(SampleWindowMs);
        var second = Sample();
        stopwatch.Stop();

        long elapsedTicks = stopwatch.Elapsed.Ticks;
        if (elapsedTicks <= 0 || first.Count == 0) return Array.Empty<CpuProcessItem>();
        int cores = Environment.ProcessorCount;

        var entries = new List<(string Name, double Usage)>(second.Count);
        foreach (var (id, (name, endTicks)) in second)
        {
            // 采样窗口内重启过的同名进程（PID 复用）起点对不上，跳过
            if (!first.TryGetValue(id, out var start) || start.Name != name) continue;
            double usage = (endTicks - start.Ticks) * 100.0 / elapsedTicks / cores;
            if (usage > 0) entries.Add((name, usage));
        }
        return SummarizeProcesses(entries, top);
    });

    /// <summary>记录每个进程的 PID、名称与累计处理器时间（Tick）。</summary>
    private static Dictionary<int, (string Name, long Ticks)> Sample()
    {
        var map = new Dictionary<int, (string, long)>();
        foreach (var process in Process.GetProcesses())
        {
            try { map[process.Id] = (process.ProcessName, process.TotalProcessorTime.Ticks); }
            catch { /* 已退出或无权限的进程跳过 */ }
            finally { process.Dispose(); }
        }
        return map;
    }

    /// <summary>按进程名合并 CPU 占用，降序取前 top 项；多进程时名称标注 ×N。</summary>
    public static List<CpuProcessItem> SummarizeProcesses(IEnumerable<(string Name, double Usage)> processes, int top)
        => processes
            .GroupBy(p => p.Name)
            .Select(g => new CpuProcessItem(g.Count() > 1 ? $"{g.Key} ×{g.Count()}" : g.Key, g.Sum(p => p.Usage)))
            .OrderByDescending(item => item.Usage)
            .Take(top)
            .ToList();
}

using System.Diagnostics;
using System.IO;
using AiOptimize.Models;
using AiOptimize.Native;

namespace AiOptimize.Services;

/// <summary>每秒采集 CPU/内存/磁盘指标并通过事件推送。</summary>
public sealed class SystemMonitorService : IDisposable
{
    private readonly PerformanceCounter _cpuCounter = new("Processor", "% Processor Time", "_Total");
    private readonly CancellationTokenSource _cts = new();
    private Task? _runTask;

    public event Action<SystemSnapshot>? SnapshotUpdated;

    public void Start() => _runTask = RunAsync(_cts.Token);

    private async Task RunAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                SystemSnapshot snapshot;
                try { snapshot = Capture(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SystemMonitor] 采集失败：{ex.Message}");
                    continue; // 单次采集失败不终止监控循环
                }
                try { SnapshotUpdated?.Invoke(snapshot); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SystemMonitor] 事件订阅方异常：{ex.Message}");
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    public SystemSnapshot Capture()
    {
        double cpu = 0;
        try { cpu = Math.Clamp(_cpuCounter.NextValue(), 0, 100); } catch { }

        var mem = NativeMethods.MEMORYSTATUSEX.Create();
        ulong total = 0, used = 0;
        if (NativeMethods.GlobalMemoryStatusEx(ref mem))
        {
            total = mem.ullTotalPhys;
            used = mem.ullTotalPhys - mem.ullAvailPhys;
        }

        var disks = new List<DiskUsageInfo>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.DriveType != DriveType.Fixed || !drive.IsReady) continue;
                disks.Add(new DiskUsageInfo(
                    drive.Name.TrimEnd('\\'),
                    drive.TotalSize - drive.TotalFreeSpace,
                    drive.TotalSize));
            }
            catch { }
        }

        return new SystemSnapshot(cpu, used, total, disks);
    }

    public void Dispose()
    {
        _cts.Cancel();
        // 等采集循环退出后再释放计数器，避免 NextValue 与 Dispose 竞态
        try { _runTask?.Wait(TimeSpan.FromSeconds(3)); } catch { }
        _cts.Dispose();
        _cpuCounter.Dispose();
    }
}

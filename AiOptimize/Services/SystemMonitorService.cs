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

    public event Action<SystemSnapshot>? SnapshotUpdated;

    public void Start() => _ = RunAsync(_cts.Token);

    private async Task RunAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                SnapshotUpdated?.Invoke(Capture());
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
        _cts.Dispose();
        _cpuCounter.Dispose();
    }
}

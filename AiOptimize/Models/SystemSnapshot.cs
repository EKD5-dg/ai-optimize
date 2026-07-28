namespace AiOptimize.Models;

public sealed record DiskUsageInfo(string Name, long UsedBytes, long TotalBytes)
{
    public long FreeBytes => TotalBytes - UsedBytes;

    public double Usage => TotalBytes == 0 ? 0 : UsedBytes * 100.0 / TotalBytes;
}

public sealed record SystemSnapshot(
    double CpuUsage,
    ulong MemoryUsedBytes,
    ulong MemoryTotalBytes,
    IReadOnlyList<DiskUsageInfo> Disks)
{
    public double MemoryUsage => MemoryTotalBytes == 0 ? 0 : MemoryUsedBytes * 100.0 / MemoryTotalBytes;
}

using AiOptimize.Models;

namespace AiOptimize.Tests;

public class DiskUsageInfoTests
{
    [Fact]
    public void FreeBytes_IsTotalMinusUsed()
    {
        var disk = new DiskUsageInfo("C:", UsedBytes: 300, TotalBytes: 1000);
        Assert.Equal(700, disk.FreeBytes);
    }

    [Fact]
    public void Usage_IsUsedPercentage()
    {
        var disk = new DiskUsageInfo("C:", UsedBytes: 250, TotalBytes: 1000);
        Assert.Equal(25.0, disk.Usage);
    }
}

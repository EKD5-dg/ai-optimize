using AiOptimize.Models;
using AiOptimize.Services;

namespace AiOptimize.Tests;

public class CpuUsageScannerTests
{
    [Fact]
    public void Summarize_GroupsByNameSumsUsageAndCollectsPids()
    {
        var input = new[]
        {
            new CpuProcessSample(10, "chrome", 12.5, false),
            new CpuProcessSample(20, "chrome", 7.5, false),
            new CpuProcessSample(30, "code", 3.0, true),
        };

        var result = CpuUsageScanner.SummarizeProcesses(input, top: 8);

        Assert.Equal(2, result.Count);
        Assert.Equal("chrome ×2", result[0].Name);
        Assert.Equal(20.0, result[0].Usage, 3);
        Assert.Equal(new[] { 10, 20 }, result[0].Pids);
        Assert.Equal("code", result[1].Name);
        Assert.Equal(new[] { 30 }, result[1].Pids);
    }

    [Fact]
    public void Summarize_OrdersByUsageDescendingAndTakesTop()
    {
        var input = new[]
        {
            new CpuProcessSample(1, "a", 1.0, false),
            new CpuProcessSample(2, "b", 55.0, false),
            new CpuProcessSample(3, "c", 30.0, false),
            new CpuProcessSample(4, "d", 10.0, false),
        };

        var result = CpuUsageScanner.SummarizeProcesses(input, top: 2);

        Assert.Equal(new[] { "b", "c" }, result.Select(r => r.Name));
    }

    [Fact]
    public void Summarize_EmptyInput_ReturnsEmpty()
        => Assert.Empty(CpuUsageScanner.SummarizeProcesses(Array.Empty<CpuProcessSample>(), top: 8));

    [Fact]
    public void CoolTargets_WindowlessHighUsage_GoesToKill_Windowed_GoesToDemote()
    {
        var input = new[]
        {
            new CpuProcessSample(1, "stuckupdater", 60.0, false), // 无窗口高占用 → 结束
            new CpuProcessSample(2, "chrome", 55.0, true),        // 有窗口高占用 → 降速
        };

        var (toKill, toDemote) = CpuUsageScanner.SelectCoolTargets(input, selfPid: 999);

        Assert.Single(toKill);
        Assert.Equal("stuckupdater", toKill[0].Name);
        Assert.Single(toDemote);
        Assert.Equal("chrome", toDemote[0].Name);
    }

    [Fact]
    public void CoolTargets_SkipsLowUsageSelfAndCritical()
    {
        var input = new[]
        {
            new CpuProcessSample(1, "notepad", 49.0, false), // 低于 50% 阈值
            new CpuProcessSample(2, "self", 80.0, false),    // 自己
            new CpuProcessSample(4, "csrss", 70.0, false),   // 系统关键进程
            new CpuProcessSample(5, "lsass", 70.0, false),   // 系统关键进程（大小写不敏感）
        };

        var (toKill, toDemote) = CpuUsageScanner.SelectCoolTargets(input, selfPid: 2);

        Assert.Empty(toKill);
        Assert.Empty(toDemote);
    }

    [Fact]
    public void CoolTargets_CaseInsensitiveCriticalCheck()
    {
        var input = new[] { new CpuProcessSample(1, "CSRSs", 90.0, false) };

        var (toKill, toDemote) = CpuUsageScanner.SelectCoolTargets(input, selfPid: 999);

        Assert.Empty(toKill);
        Assert.Empty(toDemote);
    }
}

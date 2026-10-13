using AiOptimize.Services;

namespace AiOptimize.Tests;

public class CpuUsageScannerTests
{
    [Fact]
    public void Summarize_GroupsByNameAndSumsUsage()
    {
        var input = new[] { ("chrome", 12.5), ("chrome", 7.5), ("code", 3.0) };

        var result = CpuUsageScanner.SummarizeProcesses(input, top: 8);

        Assert.Equal(2, result.Count);
        Assert.Equal("chrome ×2", result[0].Name);
        Assert.Equal(20.0, result[0].Usage, 3);
        Assert.Equal("code", result[1].Name);
        Assert.Equal(3.0, result[1].Usage, 3);
    }

    [Fact]
    public void Summarize_OrdersByUsageDescendingAndTakesTop()
    {
        var input = new[] { ("a", 1.0), ("b", 55.0), ("c", 30.0), ("d", 10.0) };

        var result = CpuUsageScanner.SummarizeProcesses(input, top: 2);

        Assert.Equal(new[] { "b", "c" }, result.Select(r => r.Name));
    }

    [Fact]
    public void Summarize_EmptyInput_ReturnsEmpty()
        => Assert.Empty(CpuUsageScanner.SummarizeProcesses(Array.Empty<(string, double)>(), top: 8));
}

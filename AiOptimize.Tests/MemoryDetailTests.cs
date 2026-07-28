using AiOptimize.Models;
using AiOptimize.Services;

namespace AiOptimize.Tests;

public class MemoryDetailTests
{
    [Fact]
    public void Summarize_GroupsByNameAndSums()
    {
        var input = new[] { ("msedge", 100L), ("msedge", 50L), ("chrome", 30L) };

        var result = MemoryOptimizer.SummarizeProcesses(input, top: 8);

        Assert.Equal(2, result.Count);
        Assert.Equal("msedge ×2", result[0].Name);
        Assert.Equal(150, result[0].Bytes);
        Assert.Equal("chrome", result[1].Name);
        Assert.Equal(30, result[1].Bytes);
    }

    [Fact]
    public void Summarize_OrdersByBytesDescendingAndTakesTop()
    {
        var input = new[] { ("a", 10L), ("b", 300L), ("c", 200L), ("d", 100L) };

        var result = MemoryOptimizer.SummarizeProcesses(input, top: 2);

        Assert.Equal(new[] { "b", "c" }, result.Select(r => r.Name));
    }

    [Fact]
    public void Summarize_EmptyInput_ReturnsEmpty()
        => Assert.Empty(MemoryOptimizer.SummarizeProcesses(Array.Empty<(string, long)>(), top: 8));
}

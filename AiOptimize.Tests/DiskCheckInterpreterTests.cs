using AiOptimize.Services;

namespace AiOptimize.Tests;

public class DiskCheckInterpreterTests
{
    [Fact]
    public void Interpret_ExitCode0_Healthy()
    {
        var (healthy, message) = DiskCheckInterpreter.Interpret(0);
        Assert.True(healthy);
        Assert.NotEmpty(message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Interpret_NonZeroExitCode_NotHealthy(int exitCode)
    {
        var (healthy, message) = DiskCheckInterpreter.Interpret(exitCode);
        Assert.False(healthy);
        Assert.NotEmpty(message);
    }
}

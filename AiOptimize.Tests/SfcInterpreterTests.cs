using AiOptimize.Services;

namespace AiOptimize.Tests;

public class SfcInterpreterTests
{
    [Fact]
    public void Interpret_ExitCode0_Healthy()
    {
        var (healthy, message) = SfcInterpreter.Interpret(0);
        Assert.True(healthy);
        Assert.NotEmpty(message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Interpret_NonZeroExitCode_NotHealthy(int exitCode)
    {
        var (healthy, message) = SfcInterpreter.Interpret(exitCode);
        Assert.False(healthy);
        Assert.NotEmpty(message);
    }
}

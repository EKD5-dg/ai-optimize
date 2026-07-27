using AiOptimize.Services;

namespace AiOptimize.Tests;

public class StartupApprovedStateTests
{
    [Fact]
    public void IsEnabled_NullOrEmpty_ReturnsTrue()
    {
        Assert.True(StartupApprovedState.IsEnabled(null));
        Assert.True(StartupApprovedState.IsEnabled(Array.Empty<byte>()));
    }

    [Fact]
    public void IsEnabled_EnabledValue_ReturnsTrue()
        => Assert.True(StartupApprovedState.IsEnabled(StartupApprovedState.CreateEnabledValue()));

    [Fact]
    public void IsEnabled_DisabledValue_ReturnsFalse()
        => Assert.False(StartupApprovedState.IsEnabled(StartupApprovedState.CreateDisabledValue()));

    [Fact]
    public void CreateDisabledValue_Is12BytesWithTimestamp()
    {
        var data = StartupApprovedState.CreateDisabledValue();
        Assert.Equal(12, data.Length);
        Assert.Equal(0x03, data[0]);
        Assert.NotEqual(0L, BitConverter.ToInt64(data, 4));
    }
}

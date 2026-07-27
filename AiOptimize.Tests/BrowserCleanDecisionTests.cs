using AiOptimize.Services;

namespace AiOptimize.Tests;

public class BrowserCleanDecisionTests
{
    [Fact]
    public void Decide_NoProcess_CleansDirectly()
        => Assert.Equal(BrowserCleanAction.Clean, DeepCleaner.DecideBrowserAction(anyProcess: false, anyWindow: false));

    [Fact]
    public void Decide_BackgroundOnly_ClosesThenCleans()
        => Assert.Equal(BrowserCleanAction.CloseBackgroundThenClean, DeepCleaner.DecideBrowserAction(anyProcess: true, anyWindow: false));

    [Fact]
    public void Decide_WindowOpen_Skips()
        => Assert.Equal(BrowserCleanAction.Skip, DeepCleaner.DecideBrowserAction(anyProcess: true, anyWindow: true));
}

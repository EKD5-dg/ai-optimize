using AiOptimize.Services;

namespace AiOptimize.Tests;

public class CommandPathFormatterTests
{
    [Fact]
    public void Clean_QuotedPathWithArgs_ReturnsPathOnly()
        => Assert.Equal(@"C:\Program Files (x86)\WXWork\WXWork.exe",
            CommandPathFormatter.Clean("\"C:\\Program Files (x86)\\WXWork\\WXWork.exe\" -min -autorun"));

    [Fact]
    public void Clean_UnquotedPathWithArgs_ReturnsPathOnly()
        => Assert.Equal(@"C:\Program Files (x86)\DingDing\DingtalkLauncher.exe",
            CommandPathFormatter.Clean(@"C:\Program Files (x86)\DingDing\DingtalkLauncher.exe /autorun"));

    [Fact]
    public void Clean_PlainPath_Unchanged()
        => Assert.Equal(@"C:\Windows\system32\ctfmon.exe",
            CommandPathFormatter.Clean(@"C:\Windows\system32\ctfmon.exe"));

    [Fact]
    public void Clean_EmptyOrNull_ReturnsEmpty()
    {
        Assert.Equal("", CommandPathFormatter.Clean(""));
        Assert.Equal("", CommandPathFormatter.Clean(null));
    }
}

using AiOptimize.Services;

namespace AiOptimize.Tests;

public class BlueScreenParserTests
{
    private const string RealMessage =
        "计算机已经从检测错误后重新启动。检测错误: 0x00000034 (0x000000000000029d, 0xffffffffc0000420, 0x0000000000000000, 0x0000000000000000)。已将转储保存在: C:\\Windows\\MEMORY.DMP。报告 ID: abc123。";

    [Fact]
    public void TryParse_RealChineseMessage_ExtractsCodeAndDump()
    {
        var ok = BlueScreenMessageParser.TryParse(RealMessage, out uint code, out string? dumpPath);

        Assert.True(ok);
        Assert.Equal(0x34u, code);
        Assert.Equal("C:\\Windows\\MEMORY.DMP", dumpPath);
    }

    [Fact]
    public void TryParse_MessageWithoutDumpPath_CodeOnly()
    {
        var ok = BlueScreenMessageParser.TryParse("检测错误: 0x0000007E (0x0, 0x0, 0x0, 0x0)。", out uint code, out string? dumpPath);

        Assert.True(ok);
        Assert.Equal(0x7Eu, code);
        Assert.Null(dumpPath);
    }

    [Fact]
    public void TryParse_NoStopCode_ReturnsFalse()
        => Assert.False(BlueScreenMessageParser.TryParse("没有代码的消息", out _, out _));
}

public class StopCodeKnowledgeTests
{
    [Fact]
    public void Lookup_KnownCode_ReturnsSpecificEntry()
    {
        var info = StopCodeKnowledge.Lookup(0x34);

        Assert.Equal("CACHE_MANAGER", info.Name);
        Assert.NotEmpty(info.Cause);
        Assert.NotEmpty(info.Advice);
    }

    [Fact]
    public void Lookup_UnknownCode_ReturnsGenericEntry()
    {
        var info = StopCodeKnowledge.Lookup(0xDEADBEEF);

        Assert.Equal("未知错误", info.Name);
        Assert.NotEmpty(info.Advice);
    }
}

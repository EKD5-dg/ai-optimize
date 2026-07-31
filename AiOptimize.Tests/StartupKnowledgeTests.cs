using AiOptimize.Services;

namespace AiOptimize.Tests;

public class StartupKnowledgeTests
{
    [Fact]
    public void Describe_Ctfmon_IsSystemKeep()
    {
        var info = StartupKnowledge.Describe("ctfmon", @"C:\Windows\system32\ctfmon.exe");
        Assert.Equal(StartupAdvice.Keep, info.Advice);
        Assert.NotEmpty(info.Description);
    }

    [Fact]
    public void Describe_DingTalk_IsOptional()
    {
        var info = StartupKnowledge.Describe("DingTalk", @"C:\Program Files (x86)\DingDing\DingtalkLauncher.exe /autorun");
        Assert.Equal(StartupAdvice.Optional, info.Advice);
        Assert.Contains("钉钉", info.Description);
    }

    [Fact]
    public void Describe_MatchesByCommandPath_WhenNameUnknown()
    {
        var info = StartupKnowledge.Describe("某某启动器", @"C:\Program Files\Tencent\QQNT\QQ.exe /background");
        Assert.Contains("QQ", info.Description);
    }

    [Fact]
    public void Describe_SecuritySoftware_IsKeep()
    {
        var info = StartupKnowledge.Describe("HuorongTray", @"C:\Program Files\Huorong\Sysdiag\bin\HipsTray.exe");
        Assert.Equal(StartupAdvice.Keep, info.Advice);
    }

    [Fact]
    public void Describe_Unknown_ReturnsJudgeYourself()
    {
        var info = StartupKnowledge.Describe("TotallyUnknownApp", @"D:\foo\bar.exe");
        Assert.Equal(StartupAdvice.Unknown, info.Advice);
        Assert.NotEmpty(info.Description);
    }
}

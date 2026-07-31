using AiOptimize.Services;

namespace AiOptimize.Tests;

public class StartupSearchTests
{
    [Theory]
    [InlineData("", true)]           // 空关键字显示全部
    [InlineData("   ", true)]
    [InlineData("ding", true)]       // 命中名称（忽略大小写）
    [InlineData("钉钉", true)]        // 命中说明
    [InlineData("DingDing", true)]   // 命中路径
    [InlineData("微信", false)]       // 未命中
    public void Matches_ChecksNameDescriptionAndCommand(string query, bool expected)
        => Assert.Equal(expected, StartupSearch.Matches(
            name: "DingTalk",
            description: "钉钉，不用它办公可以关闭",
            command: @"C:\Program Files (x86)\DingDing\DingtalkLauncher.exe",
            query: query));
}

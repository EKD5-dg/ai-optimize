using AiOptimize.Services;

namespace AiOptimize.Tests;

public class QuickActionTests
{
    [Fact]
    public void Catalog_EveryActionType_HasLabelAndFileName()
    {
        foreach (QuickActionType type in Enum.GetValues<QuickActionType>())
        {
            var spec = QuickActionCatalog.Get(type);
            Assert.False(string.IsNullOrWhiteSpace(spec.Label));
            Assert.False(string.IsNullOrWhiteSpace(spec.FileName));
        }
    }

    [Fact]
    public void Knowledge_KnownCode_HasQuickActions()
    {
        var info = StopCodeKnowledge.Lookup(0x34);
        Assert.NotEmpty(info.Actions);
    }

    [Fact]
    public void Knowledge_UnknownCode_HasQuickActions()
    {
        var info = StopCodeKnowledge.Lookup(0xDEADBEEF);
        Assert.NotEmpty(info.Actions);
    }
}

using AiOptimize.Services;

namespace AiOptimize.Tests;

/// <summary>
/// C 盘专项清理的分类默认值守卫：
/// 系统级大空间项（关闭休眠、删 Windows.old、DISM 清理）绝不允许变成默认勾选。
/// </summary>
public class CDriveCleanerTests
{
    [Fact]
    public void DefaultChecked_NeverIncludeSystemLevelCategories()
    {
        Assert.Empty(CDriveCleaner.DefaultCheckedCategoryIds.Intersect(CDriveCleaner.SystemLevelCategoryIds));
    }

    [Fact]
    public void CategoryIds_ArePartitionedIntoSafeAndSystemLevel()
    {
        var all = new[] { CDriveCleaner.IdDeliveryCache, CDriveCleaner.IdCrashDumps, CDriveCleaner.IdKernelDumps,
            CDriveCleaner.IdHibernation, CDriveCleaner.IdWindowsOld, CDriveCleaner.IdDismComponents };
        // 每个分类必须归属其中一组，防止新增分类漏配
        foreach (var id in all)
        {
            bool known = CDriveCleaner.DefaultCheckedCategoryIds.Contains(id)
                || CDriveCleaner.SystemLevelCategoryIds.Contains(id);
            Assert.True(known, $"分类 {id} 未归入安全组或系统级组");
        }
        Assert.Equal(all.Length,
            CDriveCleaner.DefaultCheckedCategoryIds.Count + CDriveCleaner.SystemLevelCategoryIds.Count);
    }

    [Fact]
    public void CleanupTargets_ContainExpectedDirectories()
    {
        var targets = CDriveCleaner.GetCleanupTargets();
        Assert.Equal(4, targets.Count);
        Assert.Contains(targets, t => t.EndsWith("DeliveryOptimization\\Cache", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(targets, t => t.EndsWith("CrashDumps", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(targets, t => t.EndsWith("LiveKernelReports", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(targets, t => t.EndsWith("Windows.old", StringComparison.OrdinalIgnoreCase));
    }
}

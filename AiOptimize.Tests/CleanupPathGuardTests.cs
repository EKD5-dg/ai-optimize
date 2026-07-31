using System.IO;
using AiOptimize.Services;

namespace AiOptimize.Tests;

/// <summary>
/// 清理路径白名单守卫：本工具以管理员权限递归删除，
/// 这些测试确保清理范围永远不会被误改到用户数据目录。
/// </summary>
public class CleanupPathGuardTests
{
    private static readonly string UserProfile =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string WindowsDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    private static readonly string LocalAppData =
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static readonly string ProgramData =
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

    /// <summary>所有清理目标必须位于已知安全前缀内。</summary>
    private static readonly string[] AllowedPrefixes =
    {
        Path.GetTempPath(),                                          // 用户临时目录
        Path.Combine(WindowsDir, "Temp"),                            // 系统临时目录
        Path.Combine(WindowsDir, "SoftwareDistribution", "Download"),// 更新缓存
        Path.Combine(WindowsDir, "Prefetch"),                        // 预读文件
        Path.Combine(LocalAppData, "Google", "Chrome", "User Data"), // Chrome 缓存
        Path.Combine(LocalAppData, "Microsoft", "Edge", "User Data"),// Edge 缓存
        Path.Combine(LocalAppData, @"Microsoft\Windows\Explorer"),   // 缩略图缓存
        Path.Combine(ProgramData, @"Microsoft\Windows\WER"),         // 错误报告
    };

    /// <summary>清理目标绝不能触碰的用户数据目录。</summary>
    private static readonly string[] ForbiddenPrefixes =
    {
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
        Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
        Path.Combine(UserProfile, "Downloads"),
    };

    private static IEnumerable<string> AllCleanupTargets()
    {
        foreach (var dir in TempFileCleaner.GetCleanupTargets()) yield return dir;
        foreach (var dir in DeepCleaner.GetSystemCleanupDirs()) yield return dir;
    }

    [Fact]
    public void CleanupTargets_AreAllUnderAllowedPrefixes()
    {
        var targets = AllCleanupTargets().ToList();
        Assert.NotEmpty(targets);

        foreach (var target in targets)
        {
            string full = Path.GetFullPath(target);
            bool allowed = AllowedPrefixes.Any(prefix =>
                full.StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(prefix)),
                    StringComparison.OrdinalIgnoreCase));
            Assert.True(allowed, $"清理目标不在安全白名单内：{full}");
        }
    }

    [Fact]
    public void CleanupTargets_NeverTouchUserDataDirectories()
    {
        foreach (var target in AllCleanupTargets())
        {
            string full = Path.GetFullPath(target);
            foreach (var forbidden in ForbiddenPrefixes)
            {
                if (string.IsNullOrEmpty(forbidden)) continue;
                string forbiddenFull = Path.TrimEndingDirectorySeparator(Path.GetFullPath(forbidden));
                bool touches = full.StartsWith(forbiddenFull, StringComparison.OrdinalIgnoreCase)
                    || forbiddenFull.StartsWith(full, StringComparison.OrdinalIgnoreCase);
                Assert.False(touches, $"清理目标 {full} 与用户数据目录 {forbiddenFull} 存在包含关系");
            }
        }
    }

    [Fact]
    public void CleanupTargets_AreNotDriveRoots()
    {
        foreach (var target in AllCleanupTargets())
        {
            string full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(target));
            string? root = Path.GetPathRoot(full)?.TrimEnd('\\');
            Assert.False(string.Equals(full, root, StringComparison.OrdinalIgnoreCase),
                $"清理目标不能是盘符根目录：{full}");
        }
    }

    [Fact]
    public void IsSafeCleanupRoot_RejectsDangerousPaths()
    {
        Assert.False(FileCleanupHelper.IsSafeCleanupRoot(null));
        Assert.False(FileCleanupHelper.IsSafeCleanupRoot(""));
        Assert.False(FileCleanupHelper.IsSafeCleanupRoot("   "));
        Assert.False(FileCleanupHelper.IsSafeCleanupRoot("relative\\path"));
        Assert.False(FileCleanupHelper.IsSafeCleanupRoot(@"C:\"));
        Assert.False(FileCleanupHelper.IsSafeCleanupRoot(
            Path.GetPathRoot(WindowsDir) ?? @"C:\"));
    }

    [Fact]
    public void IsSafeCleanupRoot_AcceptsNormalDirectory()
    {
        Assert.True(FileCleanupHelper.IsSafeCleanupRoot(Path.GetTempPath()));
    }
}

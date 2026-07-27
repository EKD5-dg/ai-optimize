using System.Diagnostics;
using System.IO;
using AiOptimize.Models;

namespace AiOptimize.Services;

/// <summary>深度垃圾清理：浏览器缓存、更新缓存、缩略图、错误报告、预读文件。</summary>
public sealed class DeepCleaner
{
    private static string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static string WindowsDir => Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    private static string ProgramData => Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

    private sealed record BrowserTarget(string DisplayName, string ProcessName, string UserDataDir);

    private static readonly BrowserTarget[] Browsers =
    {
        new("Chrome", "chrome", Path.Combine(LocalAppData, "Google", "Chrome", "User Data")),
        new("Edge", "msedge", Path.Combine(LocalAppData, "Microsoft", "Edge", "User Data")),
    };

    public Task<long> ScanAsync() => Task.Run(() =>
    {
        long size = 0;
        foreach (var browser in Browsers)
        {
            foreach (var cacheDir in GetBrowserCacheDirs(browser))
                size += FileCleanupHelper.GetDirectorySize(cacheDir);
        }
        size += FileCleanupHelper.GetDirectorySize(Path.Combine(WindowsDir, "SoftwareDistribution", "Download"));
        size += FileCleanupHelper.GetFilesSize(Path.Combine(LocalAppData, @"Microsoft\Windows\Explorer"), "thumbcache_*.db");
        size += FileCleanupHelper.GetDirectorySize(Path.Combine(ProgramData, @"Microsoft\Windows\WER\ReportQueue"));
        size += FileCleanupHelper.GetDirectorySize(Path.Combine(ProgramData, @"Microsoft\Windows\WER\ReportArchive"));
        size += FileCleanupHelper.GetFilesSize(Path.Combine(WindowsDir, "Prefetch"), "*.pf");
        return size;
    });

    public Task<CleanResult> CleanAsync() => Task.Run(() =>
    {
        var result = new CleanResult();
        foreach (var browser in Browsers)
        {
            if (Process.GetProcessesByName(browser.ProcessName).Length > 0)
            {
                result.Notes.Add($"{browser.DisplayName} 正在运行，已跳过其缓存");
                continue;
            }
            foreach (var cacheDir in GetBrowserCacheDirs(browser))
                FileCleanupHelper.DeleteDirectoryContents(cacheDir, result);
        }

        FileCleanupHelper.DeleteDirectoryContents(Path.Combine(WindowsDir, "SoftwareDistribution", "Download"), result);
        FileCleanupHelper.DeleteFiles(Path.Combine(LocalAppData, @"Microsoft\Windows\Explorer"), "thumbcache_*.db", result);
        FileCleanupHelper.DeleteDirectoryContents(Path.Combine(ProgramData, @"Microsoft\Windows\WER\ReportQueue"), result);
        FileCleanupHelper.DeleteDirectoryContents(Path.Combine(ProgramData, @"Microsoft\Windows\WER\ReportArchive"), result);
        FileCleanupHelper.DeleteFiles(Path.Combine(WindowsDir, "Prefetch"), "*.pf", result);
        return result;
    });

    private static IEnumerable<string> GetBrowserCacheDirs(BrowserTarget browser)
    {
        if (!Directory.Exists(browser.UserDataDir)) yield break;
        List<string> profiles = new();
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(browser.UserDataDir))
            {
                var name = Path.GetFileName(dir);
                if (name == "Default" || name.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase))
                    profiles.Add(dir);
            }
        }
        catch { yield break; }

        foreach (var profile in profiles)
        {
            yield return Path.Combine(profile, "Cache");
            yield return Path.Combine(profile, "Code Cache");
        }
    }
}

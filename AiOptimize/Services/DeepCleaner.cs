using System.Diagnostics;
using System.IO;
using AiOptimize.Models;

namespace AiOptimize.Services;

public enum BrowserCleanAction
{
    /// <summary>未运行，直接清理</summary>
    Clean,
    /// <summary>仅后台驻留（无窗口），先结束进程再清理</summary>
    CloseBackgroundThenClean,
    /// <summary>有窗口正在使用，跳过</summary>
    Skip,
}

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

    public Task<IReadOnlyList<ScanItem>> ScanDetailsAsync() => Task.Run<IReadOnlyList<ScanItem>>(() =>
    {
        var items = new List<ScanItem>();
        foreach (var browser in Browsers)
        {
            long size = GetBrowserCacheDirs(browser).Sum(FileCleanupHelper.GetDirectorySize);
            items.Add(new($"{browser.DisplayName} 浏览器缓存", size));
        }
        items.Add(new("Windows 更新缓存",
            FileCleanupHelper.GetDirectorySize(Path.Combine(WindowsDir, "SoftwareDistribution", "Download"))));
        items.Add(new("缩略图缓存",
            FileCleanupHelper.GetFilesSize(Path.Combine(LocalAppData, @"Microsoft\Windows\Explorer"), "thumbcache_*.db")));
        items.Add(new("系统错误报告",
            FileCleanupHelper.GetDirectorySize(Path.Combine(ProgramData, @"Microsoft\Windows\WER\ReportQueue"))
            + FileCleanupHelper.GetDirectorySize(Path.Combine(ProgramData, @"Microsoft\Windows\WER\ReportArchive"))));
        items.Add(new("预读文件",
            FileCleanupHelper.GetFilesSize(Path.Combine(WindowsDir, "Prefetch"), "*.pf")));
        return items;
    });

    public Task<CleanResult> CleanAsync() => Task.Run(() =>
    {
        var result = new CleanResult();
        foreach (var browser in Browsers)
        {
            var processes = Process.GetProcessesByName(browser.ProcessName);
            bool anyWindow = processes.Any(p => { try { return p.MainWindowHandle != IntPtr.Zero; } catch { return false; } });
            var action = DecideBrowserAction(processes.Length > 0, anyWindow);

            switch (action)
            {
                case BrowserCleanAction.Skip:
                    result.Notes.Add($"{browser.DisplayName} 正在使用中，已跳过其缓存（关闭浏览器后可清理）");
                    break;
                case BrowserCleanAction.CloseBackgroundThenClean:
                    CloseProcesses(processes);
                    foreach (var cacheDir in GetBrowserCacheDirs(browser))
                        FileCleanupHelper.DeleteDirectoryContents(cacheDir, result);
                    result.Notes.Add($"已结束 {browser.DisplayName} 后台驻留进程并清理其缓存");
                    break;
                default:
                    foreach (var cacheDir in GetBrowserCacheDirs(browser))
                        FileCleanupHelper.DeleteDirectoryContents(cacheDir, result);
                    break;
            }

            foreach (var p in processes) p.Dispose();
        }

        FileCleanupHelper.DeleteDirectoryContents(Path.Combine(WindowsDir, "SoftwareDistribution", "Download"), result);
        FileCleanupHelper.DeleteFiles(Path.Combine(LocalAppData, @"Microsoft\Windows\Explorer"), "thumbcache_*.db", result);
        FileCleanupHelper.DeleteDirectoryContents(Path.Combine(ProgramData, @"Microsoft\Windows\WER\ReportQueue"), result);
        FileCleanupHelper.DeleteDirectoryContents(Path.Combine(ProgramData, @"Microsoft\Windows\WER\ReportArchive"), result);
        FileCleanupHelper.DeleteFiles(Path.Combine(WindowsDir, "Prefetch"), "*.pf", result);
        return result;
    });

    /// <summary>浏览器缓存清理策略：有窗口=跳过；仅后台驻留=先结束再清；未运行=直接清。</summary>
    public static BrowserCleanAction DecideBrowserAction(bool anyProcess, bool anyWindow)
    {
        if (!anyProcess) return BrowserCleanAction.Clean;
        return anyWindow ? BrowserCleanAction.Skip : BrowserCleanAction.CloseBackgroundThenClean;
    }

    private static void CloseProcesses(Process[] processes)
    {
        foreach (var p in processes)
        {
            try { p.Kill(); } catch { }
        }
        foreach (var p in processes)
        {
            try { p.WaitForExit(3000); } catch { }
        }
    }

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

using System.IO;
using AiOptimize.Models;
using Microsoft.Win32;

namespace AiOptimize.Services;

/// <summary>启动项枚举与启用/禁用（只写 StartupApproved 状态位，不删除原始项）。</summary>
public sealed class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunKeyWow64 = @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedRun = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string ApprovedRun32 = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32";
    private const string ApprovedFolder = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";

    public List<StartupItem> GetItems()
    {
        var items = new List<StartupItem>();
        CollectRegistryItems(Registry.CurrentUser, RunKey, ApprovedRun, StartupSource.HkcuRun, items);
        CollectRegistryItems(Registry.LocalMachine, RunKey, ApprovedRun, StartupSource.HklmRun, items);
        CollectRegistryItems(Registry.LocalMachine, RunKeyWow64, ApprovedRun32, StartupSource.HklmRunWow64, items);
        CollectFolderItems(Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            Registry.CurrentUser, StartupSource.UserStartupFolder, items);
        CollectFolderItems(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
            Registry.LocalMachine, StartupSource.CommonStartupFolder, items);
        return items;
    }

    public void SetEnabled(StartupItem item, bool enabled)
    {
        var (root, approvedPath) = item.Source switch
        {
            StartupSource.HkcuRun => (Registry.CurrentUser, ApprovedRun),
            StartupSource.HklmRun => (Registry.LocalMachine, ApprovedRun),
            StartupSource.HklmRunWow64 => (Registry.LocalMachine, ApprovedRun32),
            StartupSource.UserStartupFolder => (Registry.CurrentUser, ApprovedFolder),
            StartupSource.CommonStartupFolder => (Registry.LocalMachine, ApprovedFolder),
            _ => throw new InvalidOperationException($"未知来源: {item.Source}"),
        };
        using var key = root.CreateSubKey(approvedPath, writable: true)
            ?? throw new InvalidOperationException("无法打开 StartupApproved 注册表键");
        key.SetValue(item.Name,
            enabled ? StartupApprovedState.CreateEnabledValue() : StartupApprovedState.CreateDisabledValue(),
            RegistryValueKind.Binary);
        item.IsEnabled = enabled;
    }

    private static void CollectRegistryItems(RegistryKey root, string runPath, string approvedPath,
        StartupSource source, List<StartupItem> items)
    {
        using var runKey = root.OpenSubKey(runPath);
        if (runKey is null) return;
        using var approvedKey = root.OpenSubKey(approvedPath);
        foreach (var name in runKey.GetValueNames())
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            items.Add(new StartupItem
            {
                Name = name,
                Command = runKey.GetValue(name)?.ToString() ?? "",
                Source = source,
                IsEnabled = StartupApprovedState.IsEnabled(approvedKey?.GetValue(name) as byte[]),
            });
        }
    }

    private static void CollectFolderItems(string folder, RegistryKey approvedRoot,
        StartupSource source, List<StartupItem> items)
    {
        if (!Directory.Exists(folder)) return;
        using var approvedKey = approvedRoot.OpenSubKey(ApprovedFolder);
        foreach (var file in Directory.EnumerateFiles(folder))
        {
            var fileName = Path.GetFileName(file);
            if (fileName.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
            items.Add(new StartupItem
            {
                Name = fileName,
                Command = file,
                Source = source,
                IsEnabled = StartupApprovedState.IsEnabled(approvedKey?.GetValue(fileName) as byte[]),
            });
        }
    }
}

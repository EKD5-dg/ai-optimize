using System.IO;
using System.Management;
using Microsoft.Win32;

namespace AiOptimize.Services;

/// <summary>环境风险提示的严重程度。</summary>
public enum ContextSeverity { High, Medium, Info }

/// <summary>一条环境风险提示。</summary>
public sealed record ContextHint(string Text, ContextSeverity Severity);

/// <summary>蓝屏分析的环境上下文：显卡/虚拟显示驱动、转储文件、崩溃相关系统设置。</summary>
public sealed record BlueScreenContext(IReadOnlyList<ContextHint> Hints, IReadOnlyList<string> VideoControllers);

/// <summary>
/// 环境上下文探针：把"黑屏/蓝屏高发因素"查出来，供蓝屏分析页顶部展示。
/// 所有查询都容错：单项失败不影响其他项。
/// </summary>
public static class BlueScreenContextProbe
{
    // 虚拟显示驱动特征名：远程控制(向日葵 OrayIdd)/模拟器(MuMu)等第三方间接显示驱动
    private static readonly string[] VirtualDisplayKeywords = { "oray", "mumu", "idd", "virtual display", "idm", "indirect display" };

    public static Task<BlueScreenContext> ProbeAsync() => Task.Run(Probe);

    private static BlueScreenContext Probe()
    {
        var hints = new List<ContextHint>();
        var controllers = new List<string>();

        ProbeVideoControllers(controllers, hints);
        ProbeMinidump(hints);
        ProbeAutoReboot(hints);
        ProbeKernelPower(hints);

        if (hints.Count == 0)
        {
            hints.Add(new ContextHint("未发现明显的环境风险项，系统环境基本健康。", ContextSeverity.Info));
        }
        return new BlueScreenContext(hints, controllers);
    }

    /// <summary>显卡列表 + 虚拟显示驱动/驱动过旧提示。</summary>
    private static void ProbeVideoControllers(List<string> controllers, List<ContextHint> hints)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, DriverDate FROM Win32_VideoController");
            foreach (var obj in searcher.Get())
            {
                string name = obj["Name"]?.ToString() ?? "";
                if (name.Length == 0) continue;
                controllers.Add(name);

                bool isVirtual = VirtualDisplayKeywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));
                if (isVirtual)
                {
                    hints.Add(new ContextHint(
                        $"检测到虚拟显示驱动「{name}」：这类驱动（远程控制/模拟器用）是黑屏与蓝屏的高发原因，建议更新或临时禁用。",
                        ContextSeverity.High));
                }

                if (obj["DriverDate"] is DateTime driverDate && (DateTime.Now - driverDate).TotalDays > 365)
                {
                    hints.Add(new ContextHint(
                        $"显卡驱动「{name}」安装于 {driverDate:yyyy/MM}，已超过一年，建议检查是否有新版本。",
                        ContextSeverity.Medium));
                }
            }
        }
        catch { /* WMI 不可用时跳过 */ }
    }

    /// <summary>蓝屏转储文件检查：转储被清理会导致无法精确定位崩溃驱动。</summary>
    private static void ProbeMinidump(List<ContextHint> hints)
    {
        try
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Minidump");
            if (!Directory.Exists(dir))
            {
                hints.Add(new ContextHint("系统未开启蓝屏转储（Minidump 目录不存在），崩溃时将无法定位出错驱动。", ContextSeverity.Medium));
                return;
            }
            if (!Directory.EnumerateFiles(dir).Any())
            {
                hints.Add(new ContextHint("蓝屏转储目录是空的：崩溃证据可能被清理工具删除，导致无法精确定位出错的驱动文件。请勿清理 C:\\Windows\\Minidump。", ContextSeverity.Medium));
            }
        }
        catch { /* 权限不足时跳过 */ }
    }

    /// <summary>崩溃后自动重启设置：开启会让人看不到蓝屏信息。</summary>
    private static void ProbeAutoReboot(List<ContextHint> hints)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\CrashControl");
            int autoReboot = key?.GetValue("AutoReboot") as int? ?? 1;
            if (autoReboot == 1)
            {
                hints.Add(new ContextHint("系统设置为「蓝屏后自动重启」，会直接黑屏重启而看不到蓝屏信息。可在系统属性 → 启动和故障恢复中关闭。", ContextSeverity.Medium));
            }
        }
        catch { /* 注册表不可读时跳过 */ }
    }

    /// <summary>近 90 天非正常关机（Event 41）统计：与蓝屏记录互相印证崩溃是否反复。</summary>
    private static void ProbeKernelPower(List<ContextHint> hints)
    {
        try
        {
            var since = DateTime.Now.AddDays(-90);
            using var reader = new System.Diagnostics.Eventing.Reader.EventLogReader(
                new System.Diagnostics.Eventing.Reader.EventLogQuery(
                    "System", System.Diagnostics.Eventing.Reader.PathType.LogName,
                    $"*[System[Provider[@Name='Microsoft-Windows-Kernel-Power'] and (EventID=41) and TimeCreated[timediff(@SystemTime) &lt;= 7776000000]]]"));
            int count = 0;
            while (reader.ReadEvent() is not null) count++;
            if (count >= 2)
            {
                hints.Add(new ContextHint(
                    $"近 90 天发生 {count} 次非正常关机（Event 41），与蓝屏记录互相印证：崩溃反复出现，值得认真排查。",
                    ContextSeverity.High));
            }
        }
        catch { /* 日志不可读时跳过 */ }
    }
}

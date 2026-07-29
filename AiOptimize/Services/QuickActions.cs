using System.Diagnostics;

namespace AiOptimize.Services;

public enum QuickActionType
{
    /// <summary>在线磁盘检查（无需重启）</summary>
    DiskCheck,
    /// <summary>Windows 内存诊断</summary>
    MemoryDiagnostic,
    /// <summary>系统文件检查修复</summary>
    SfcScan,
    /// <summary>打开设备管理器（更新驱动）</summary>
    DeviceManager,
    /// <summary>打开 Windows 更新设置</summary>
    WindowsUpdate,
}

public sealed record QuickActionSpec(string Label, string FileName, string? Arguments, bool UseShellExecute);

/// <summary>蓝屏建议对应的一键操作：标签与启动方式。</summary>
public static class QuickActionCatalog
{
    private static readonly Dictionary<QuickActionType, QuickActionSpec> Specs = new()
    {
        [QuickActionType.DiskCheck] = new("一键磁盘检查", "cmd.exe",
            "/k title 磁盘检查 && echo 正在在线检查系统盘（无需重启，请勿关闭窗口）... && chkdsk C: /scan", false),
        [QuickActionType.MemoryDiagnostic] = new("运行内存诊断", "MdSched.exe", null, true),
        [QuickActionType.SfcScan] = new("修复系统文件", "cmd.exe",
            "/k title 系统文件修复 && echo 正在扫描并修复系统文件（可能需要十几分钟，请勿关闭窗口）... && sfc /scannow", false),
        [QuickActionType.DeviceManager] = new("打开设备管理器", "devmgmt.msc", null, true),
        [QuickActionType.WindowsUpdate] = new("检查系统更新", "ms-settings:windowsupdate", null, true),
    };

    public static QuickActionSpec Get(QuickActionType type) => Specs[type];

    public static void Launch(QuickActionType type)
    {
        var spec = Get(type);
        var startInfo = new ProcessStartInfo
        {
            FileName = spec.FileName,
            Arguments = spec.Arguments ?? "",
            UseShellExecute = spec.UseShellExecute,
        };
        Process.Start(startInfo)?.Dispose();
    }
}

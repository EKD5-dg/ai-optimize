using System.Diagnostics;
using System.IO;

namespace AiOptimize.Services;

public sealed record DiskCheckResult(bool IsHealthy, string Message);

/// <summary>根据 chkdsk 返回码给出大白话结论。</summary>
public static class DiskCheckInterpreter
{
    public static (bool Healthy, string Message) Interpret(int exitCode) => exitCode switch
    {
        0 => (true, "磁盘检查完成，没有发现问题，磁盘状态良好。"),
        1 => (false, "发现磁盘错误，建议安排修复（下次开机时自动修复）。"),
        2 => (false, "磁盘需要清理，建议安排修复。"),
        3 => (false, "磁盘存在无法在线修复的问题，建议安排修复（下次开机时自动修复）。"),
        _ => (false, $"检查结果异常（代码 {exitCode}），建议安排修复。"),
    };
}

/// <summary>执行 chkdsk 在线扫描并返回结构化结果。</summary>
public static class DiskCheckRunner
{
    /// <summary>系统盘盘符（如 "C:"），从 Windows 目录推导，不硬编码。</summary>
    internal static string SystemDrive =>
        Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows))?.TrimEnd('\\') ?? "C:";

    /// <summary>对系统盘执行在线只读扫描（chkdsk /scan），不修改磁盘。支持取消（杀进程）。</summary>
    public static async Task<DiskCheckResult> RunScanAsync(CancellationToken cancellationToken = default)
    {
        Process? process = null;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "chkdsk.exe",
                Arguments = $"{SystemDrive} /scan",
                UseShellExecute = false,
                CreateNoWindow = true,
                // 不重定向输出流：chkdsk 输出量大，重定向不读取会导致管道写满死锁
            };
            process = Process.Start(psi) ?? throw new Exception("无法启动 chkdsk");
            using (process)
            {
                await process.WaitForExitAsync(cancellationToken);
                var (healthy, message) = DiskCheckInterpreter.Interpret(process.ExitCode);
                return new DiskCheckResult(healthy, message);
            }
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            return new DiskCheckResult(false, "磁盘检查已取消。");
        }
        catch (Exception ex)
        {
            return new DiskCheckResult(false, $"磁盘检查未能完成：{ex.Message}");
        }
    }

    private static void TryKill(Process? process)
    {
        try { if (process is { HasExited: false }) process.Kill(); } catch { }
    }

    /// <summary>安排下次启动时自动修复：为系统盘置 dirty 位，重启后由系统自动执行 chkdsk。</summary>
    public static async Task<DiskCheckResult> ScheduleRepairAsync()
    {
        try
        {
            // fsutil dirty set 是确定性排期方式，无需解析交互式提示
            var psi = new ProcessStartInfo
            {
                FileName = "fsutil.exe",
                Arguments = "dirty set C:",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(psi) ?? throw new Exception("无法启动 fsutil");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await process.WaitForExitAsync(cts.Token);
            if (process.ExitCode != 0)
                return new DiskCheckResult(false, $"安排修复失败（代码 {process.ExitCode}），请确认以管理员身份运行。");
            return new DiskCheckResult(true, "已安排修复。下次重启电脑时系统会自动检查并修复磁盘，重启后耐心等待即可。");
        }
        catch (OperationCanceledException)
        {
            return new DiskCheckResult(false, "安排修复超时，请重试。");
        }
        catch (Exception ex)
        {
            return new DiskCheckResult(false, $"安排修复失败：{ex.Message}");
        }
    }
}

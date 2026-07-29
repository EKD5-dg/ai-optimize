using System.Diagnostics;

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
    /// <summary>对 C 盘执行在线只读扫描（chkdsk /scan），不修改磁盘。</summary>
    public static async Task<DiskCheckResult> RunScanAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "chkdsk.exe",
                Arguments = "C: /scan",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var process = Process.Start(psi) ?? throw new Exception("无法启动 chkdsk");
            await process.WaitForExitAsync();
            var (healthy, message) = DiskCheckInterpreter.Interpret(process.ExitCode);
            return new DiskCheckResult(healthy, message);
        }
        catch (Exception ex)
        {
            return new DiskCheckResult(false, $"磁盘检查未能完成：{ex.Message}");
        }
    }

    /// <summary>安排下次启动时自动修复（chkdsk /r /x，需重启后执行）。</summary>
    public static DiskCheckResult ScheduleRepair()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "chkdsk.exe",
                Arguments = "C: /r /x",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
            };
            using var process = Process.Start(psi) ?? throw new Exception("无法启动 chkdsk");
            // chkdsk 对系统盘会提示"下次重启检查"，发送 Y 确认
            process.StandardInput.WriteLine("Y");
            process.WaitForExit(15000);
            return new DiskCheckResult(true, "已安排修复。下次重启电脑时系统会自动检查并修复磁盘，重启后耐心等待即可。");
        }
        catch (Exception ex)
        {
            return new DiskCheckResult(false, $"安排修复失败：{ex.Message}");
        }
    }
}

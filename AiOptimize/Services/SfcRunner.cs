using System.Diagnostics;

namespace AiOptimize.Services;

/// <summary>系统文件检查（sfc /scannow）：后台执行并给出大白话结论。</summary>
public static class SfcInterpreter
{
    public static (bool Healthy, string Message) Interpret(int exitCode) => exitCode switch
    {
        0 => (true, "系统文件检查完成：文件完好，或已自动修复全部问题。"),
        _ => (false, "检查完成，但可能存在未能修复的问题。建议重启电脑后再运行一次；若仍有提示，可以把情况告诉帮你安装本软件的人。"),
    };
}

public static class SfcRunner
{
    public static async Task<DiskCheckResult> RunAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sfc.exe",
                Arguments = "/scannow",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var process = Process.Start(psi) ?? throw new Exception("无法启动系统文件检查");
            await process.WaitForExitAsync();
            var (healthy, message) = SfcInterpreter.Interpret(process.ExitCode);
            return new DiskCheckResult(healthy, message);
        }
        catch (Exception ex)
        {
            return new DiskCheckResult(false, $"系统文件检查未能完成：{ex.Message}");
        }
    }
}

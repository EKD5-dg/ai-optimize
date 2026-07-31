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
    public static async Task<DiskCheckResult> RunAsync(CancellationToken cancellationToken = default)
    {
        Process? process = null;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sfc.exe",
                Arguments = "/scannow",
                UseShellExecute = false,
                CreateNoWindow = true,
                // 不重定向输出流：sfc 持续输出进度（UTF-16），重定向不读取会导致管道写满死锁
            };
            process = Process.Start(psi) ?? throw new Exception("无法启动系统文件检查");
            using (process)
            {
                await process.WaitForExitAsync(cancellationToken);
                var (healthy, message) = SfcInterpreter.Interpret(process.ExitCode);
                return new DiskCheckResult(healthy, message);
            }
        }
        catch (OperationCanceledException)
        {
            try { if (process is { HasExited: false }) process.Kill(); } catch { }
            return new DiskCheckResult(false, "系统文件检查已取消。");
        }
        catch (Exception ex)
        {
            return new DiskCheckResult(false, $"系统文件检查未能完成：{ex.Message}");
        }
    }
}

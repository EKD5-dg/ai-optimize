using System.Diagnostics;
using System.IO;
using AiOptimize.Models;
using AiOptimize.Utils;

namespace AiOptimize.Services;

/// <summary>
/// C 盘专项清理：传递优化缓存、崩溃转储等纯缓存默认勾选；
/// 关闭休眠、删除 Windows.old、DISM 组件清理属于系统级大空间项，必须用户手动勾选。
/// 刻意不清理 Minidump/MEMORY.DMP，保留蓝屏分析所需的现场证据。
/// </summary>
public sealed class CDriveCleaner
{
    internal const string IdDeliveryCache = "delivery_cache";
    internal const string IdCrashDumps = "crash_dumps";
    internal const string IdKernelDumps = "kernel_dumps";
    internal const string IdHibernation = "hibernation";
    internal const string IdWindowsOld = "windows_old";
    internal const string IdDismComponents = "dism_components";

    /// <summary>默认勾选的纯缓存分类（供测试校验默认值不会被误改成危险项）。</summary>
    internal static readonly IReadOnlyList<string> DefaultCheckedCategoryIds =
        new[] { IdDeliveryCache, IdCrashDumps, IdKernelDumps };

    /// <summary>有系统级影响的分类，绝不允许加入默认勾选。</summary>
    internal static readonly IReadOnlyList<string> SystemLevelCategoryIds =
        new[] { IdHibernation, IdWindowsOld, IdDismComponents };

    private static string WindowsDir => Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    private static string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static string SystemDriveRoot => Path.GetPathRoot(WindowsDir) ?? @"C:\";

    private static string DeliveryOptimizationCache =>
        Path.Combine(WindowsDir, @"ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache");
    private static string CrashDumpsDir => Path.Combine(LocalAppData, "CrashDumps");
    private static string LiveKernelReportsDir => Path.Combine(WindowsDir, "LiveKernelReports");
    private static string WindowsOldDir => Path.Combine(SystemDriveRoot, "Windows.old");
    private static string HiberfilFile => Path.Combine(SystemDriveRoot, "hiberfil.sys");

    /// <summary>目录型清理目标（供守卫测试校验，防止清理范围被误改）。</summary>
    internal static IReadOnlyList<string> GetCleanupTargets() => new[]
    {
        DeliveryOptimizationCache,
        CrashDumpsDir,
        LiveKernelReportsDir,
        WindowsOldDir,
    };

    public Task<IReadOnlyList<CleanupCategory>> ScanAsync() => Task.Run<IReadOnlyList<CleanupCategory>>(() =>
    {
        var list = new List<CleanupCategory>
        {
            new(IdDeliveryCache, "传递优化缓存",
                "Windows 更新分发时留下的缓存文件，删除后会自动重新生成",
                FileCleanupHelper.GetDirectorySize(DeliveryOptimizationCache), true),
            new(IdCrashDumps, "程序崩溃转储",
                "程序出错时生成的调试报告，不影响正常使用",
                FileCleanupHelper.GetDirectorySize(CrashDumpsDir), true),
            new(IdKernelDumps, "内核实时转储",
                "系统组件异常时留下的报告文件，可安全删除（不含蓝屏转储）",
                FileCleanupHelper.GetDirectorySize(LiveKernelReportsDir), true),
        };

        long? hiberfil = TryGetFileSize(HiberfilFile);
        if (hiberfil is not null)
            list.Add(new CleanupCategory(IdHibernation, "关闭休眠功能（释放休眠文件）",
                $"休眠文件约占用 {ByteFormatter.Format(hiberfil.Value)}；关闭后将无法使用休眠和快速启动，可随时用 powercfg /h on 恢复",
                hiberfil.Value, false));

        if (Directory.Exists(WindowsOldDir))
            list.Add(new CleanupCategory(IdWindowsOld, "删除旧系统备份 Windows.old",
                $"升级系统后保留的旧系统文件，共 {ByteFormatter.Format(FileCleanupHelper.GetDirectorySize(WindowsOldDir))}；删除后将无法回退到之前的系统版本",
                FileCleanupHelper.GetDirectorySize(WindowsOldDir), false));

        list.Add(new CleanupCategory(IdDismComponents, "DISM 组件存储深度清理（WinSxS）",
            "清理系统更新遗留的旧组件，通常能释放几个 GB；耗时较长（可能10分钟以上），期间请不要关机",
            -1, false));
        return list;
    });

    /// <summary>执行单个分类的清理。逐分类调用便于界面显示当前进度。</summary>
    public async Task<CleanResult> CleanCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        var result = new CleanResult();
        switch (categoryId)
        {
            case IdDeliveryCache:
                await Task.Run(() => FileCleanupHelper.DeleteDirectoryContents(DeliveryOptimizationCache, result), cancellationToken);
                break;
            case IdCrashDumps:
                await Task.Run(() => FileCleanupHelper.DeleteDirectoryContents(CrashDumpsDir, result), cancellationToken);
                break;
            case IdKernelDumps:
                await Task.Run(() => FileCleanupHelper.DeleteDirectoryContents(LiveKernelReportsDir, result), cancellationToken);
                break;
            case IdHibernation:
                await DisableHibernationAsync(result, cancellationToken);
                break;
            case IdWindowsOld:
                await DeleteWindowsOldAsync(result, cancellationToken);
                break;
            case IdDismComponents:
                await CleanComponentsAsync(result, cancellationToken);
                break;
        }
        return result;
    }

    private static async Task DisableHibernationAsync(CleanResult result, CancellationToken cancellationToken)
    {
        // 先量出休眠文件大小再关闭，关闭成功后该大小即释放的空间
        long before = TryGetFileSize(HiberfilFile) ?? 0;
        var (ok, exitCode) = await RunCommandAsync("powercfg.exe", "/h off", cancellationToken);
        if (!ok || exitCode != 0)
        {
            result.Notes.Add("关闭休眠失败，请确认软件以管理员身份运行");
            return;
        }
        result.BytesFreed += before;
        result.Notes.Add(before > 0
            ? $"已关闭休眠功能，释放 {ByteFormatter.Format(before)}"
            : "已关闭休眠功能");
    }

    private static async Task DeleteWindowsOldAsync(CleanResult result, CancellationToken cancellationToken)
    {
        if (!FileCleanupHelper.IsSafeCleanupRoot(WindowsOldDir) || !Directory.Exists(WindowsOldDir)) return;
        long before = FileCleanupHelper.GetDirectorySize(WindowsOldDir);

        // Windows.old 受 TrustedInstaller 保护：先取回所有权并授予管理员完全控制，再走通用容错删除。
        // 授权用 SID（*S-1-5-32-544 = Administrators），避免中文/英文系统的组名本地化差异
        await RunCommandAsync("takeown.exe", $"/f \"{WindowsOldDir}\" /r /a", cancellationToken);
        await RunCommandAsync("icacls.exe", $"\"{WindowsOldDir}\" /grant *S-1-5-32-544:(OI)(CI)F /t /c /q", cancellationToken);
        await Task.Run(() => FileCleanupHelper.DeleteDirectoryContents(WindowsOldDir, result), cancellationToken);
        try { Directory.Delete(WindowsOldDir, false); } catch { }

        long remaining = Directory.Exists(WindowsOldDir) ? FileCleanupHelper.GetDirectorySize(WindowsOldDir) : 0;
        long freed = Math.Max(0, before - remaining);
        result.BytesFreed += freed;
        result.Notes.Add(remaining > 0
            ? "Windows.old 有部分文件被占用未能删除，重启电脑后可再试一次"
            : $"Windows.old 已删除，释放 {ByteFormatter.Format(freed)}");
    }

    private static async Task CleanComponentsAsync(CleanResult result, CancellationToken cancellationToken)
    {
        long freeBefore = GetFreeBytes();
        var (ok, exitCode) = await RunCommandAsync(
            "Dism.exe", "/Online /Cleanup-Image /StartComponentCleanup", cancellationToken);

        // DISM 不输出可解析的释放量，用前后可用空间差近似
        long freed = GetFreeBytes() - freeBefore;
        if (freed > 0) result.BytesFreed += freed;
        if (!ok || exitCode != 0)
            result.Notes.Add($"组件存储清理未正常完成（代码 {exitCode}），不影响其他清理项");
        else
            result.Notes.Add($"组件存储清理完成，释放 {ByteFormatter.Format(Math.Max(0, freed))}");
    }

    private static async Task<(bool Ok, int ExitCode)> RunCommandAsync(
        string fileName, string arguments, CancellationToken cancellationToken)
    {
        Process? process = null;
        try
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (process is null) return (false, -1);
            await process.WaitForExitAsync(cancellationToken);
            return (true, process.ExitCode);
        }
        catch (OperationCanceledException)
        {
            // 取消时不留半运行的子进程（DISM 中途终止是安全的，组件操作有事务保护）
            try { if (process is { HasExited: false }) process.Kill(); } catch { }
            throw;
        }
        catch
        {
            return (false, -1);
        }
        finally
        {
            process?.Dispose();
        }
    }

    private static long GetFreeBytes()
    {
        try { return new DriveInfo(SystemDriveRoot).AvailableFreeSpace; }
        catch { return 0; }
    }

    private static long? TryGetFileSize(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return info.Exists ? info.Length : null;
        }
        catch { return null; }
    }
}

using System.IO;
using System.Runtime.InteropServices;
using AiOptimize.Models;
using AiOptimize.Native;

namespace AiOptimize.Services;

/// <summary>临时文件清理：用户临时目录、Windows\Temp、回收站。</summary>
public sealed class TempFileCleaner
{
    private static readonly string UserTempDir = Path.GetTempPath();
    private static readonly string WindowsTempDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");

    private static readonly string[] TargetDirectories = { UserTempDir, WindowsTempDir };

    public Task<IReadOnlyList<ScanItem>> ScanDetailsAsync() => Task.Run<IReadOnlyList<ScanItem>>(() => new List<ScanItem>
    {
        new("用户临时文件", FileCleanupHelper.GetDirectorySize(UserTempDir)),
        new("系统临时文件", FileCleanupHelper.GetDirectorySize(WindowsTempDir)),
        new("回收站", GetRecycleBinSize()),
    });

    public Task<CleanResult> CleanAsync() => Task.Run(() =>
    {
        var result = new CleanResult();
        foreach (var dir in TargetDirectories)
        {
            FileCleanupHelper.DeleteDirectoryContents(dir, result);
        }

        long recycleSize = GetRecycleBinSize();
        int hr = NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null,
            NativeMethods.SHERB_NOCONFIRMATION | NativeMethods.SHERB_NOPROGRESSUI | NativeMethods.SHERB_NOSOUND);
        if (hr == 0 && recycleSize > 0)
        {
            result.BytesFreed += recycleSize;
            result.Notes.Add("回收站已清空");
        }
        return result;
    });

    public static long GetRecycleBinSize()
    {
        var info = new NativeMethods.SHQUERYRBINFO
        {
            cbSize = Marshal.SizeOf<NativeMethods.SHQUERYRBINFO>(),
        };
        return NativeMethods.SHQueryRecycleBin(null, ref info) == 0 ? info.i64Size : 0;
    }
}

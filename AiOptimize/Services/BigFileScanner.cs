using System.IO;
using AiOptimize.Models;

namespace AiOptimize.Services;

/// <summary>大文件扫描：个人常用目录 + 非系统数据盘，找出最占空间的文件。</summary>
public static class BigFileScanner
{
    public const long DefaultMinBytes = 100L * 1024 * 1024; // 100 MB
    public const int DefaultTop = 50;

    /// <summary>扫描根目录：桌面/文档/下载/视频/图片/音乐 + C 盘以外的固定数据盘。</summary>
    public static IReadOnlyList<string> GetScanRoots()
    {
        var roots = new List<string>();

        void AddIfExists(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
            string normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            // 跳过已被已有根目录覆盖的路径（如文档被重定向到 D:\Docs 时不再重复扫描 D:\）
            bool covered = roots.Any(r => normalized.StartsWith(
                Path.TrimEndingDirectorySeparator(r) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase));
            if (!covered && !roots.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                roots.Add(normalized);
        }

        AddIfExists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        AddIfExists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        AddIfExists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
        AddIfExists(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
        AddIfExists(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
        AddIfExists(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));

        string systemRoot = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:\\";
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.DriveType == DriveType.Fixed && drive.IsReady &&
                    !string.Equals(drive.Name, systemRoot, StringComparison.OrdinalIgnoreCase))
                    roots.Add(drive.RootDirectory.FullName);
            }
            catch { }
        }
        return roots;
    }

    public static Task<List<BigFile>> ScanAsync(long minBytes = DefaultMinBytes, int top = DefaultTop,
        CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        var found = new List<BigFile>();
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System,
        };

        foreach (var root in GetScanRoots())
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                foreach (var file in Directory.EnumerateFiles(root, "*", options))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        if (file.Contains("$RECYCLE.BIN", StringComparison.OrdinalIgnoreCase)) continue;
                        long length = new FileInfo(file).Length;
                        if (length >= minBytes) found.Add(new BigFile(file, length));
                    }
                    catch (OperationCanceledException) { throw; }
                    catch { }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { }
        }

        return SelectTop(found, minBytes, top);
    }, cancellationToken);

    /// <summary>过滤低于阈值的文件，按大小降序取前 top 个。</summary>
    public static List<BigFile> SelectTop(IEnumerable<BigFile> files, long minBytes, int top)
        => files.Where(f => f.Bytes >= minBytes)
            .OrderByDescending(f => f.Bytes)
            .Take(top)
            .ToList();
}

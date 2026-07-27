using System.IO;
using AiOptimize.Models;

namespace AiOptimize.Services;

/// <summary>文件扫描与逐文件容错删除的基础库，所有清理服务共用。</summary>
public static class FileCleanupHelper
{
    public static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;
        long size = 0;
        foreach (var file in EnumerateFilesSafe(path))
        {
            try { size += new FileInfo(file).Length; } catch { }
        }
        return size;
    }

    public static long GetFilesSize(string directory, string pattern)
    {
        if (!Directory.Exists(directory)) return 0;
        long size = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, pattern))
            {
                try { size += new FileInfo(file).Length; } catch { }
            }
        }
        catch { }
        return size;
    }

    /// <summary>删除目录下全部内容（保留目录本身），逐文件容错。</summary>
    public static void DeleteDirectoryContents(string path, CleanResult result)
    {
        if (!Directory.Exists(path)) return;
        foreach (var file in EnumerateFilesSafe(path))
        {
            TryDeleteFile(file, result);
        }
        try
        {
            // 由深到浅删除已清空的子目录
            foreach (var dir in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories)
                         .OrderByDescending(d => d.Length))
            {
                try { Directory.Delete(dir, false); } catch { }
            }
        }
        catch { }
    }

    public static void DeleteFiles(string directory, string pattern, CleanResult result)
    {
        if (!Directory.Exists(directory)) return;
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, pattern))
            {
                TryDeleteFile(file, result);
            }
        }
        catch { }
    }

    private static void TryDeleteFile(string file, CleanResult result)
    {
        try
        {
            var info = new FileInfo(file);
            long length = info.Length;
            info.Attributes = FileAttributes.Normal;
            info.Delete();
            result.BytesFreed += length;
            result.FilesDeleted++;
        }
        catch
        {
            result.FilesSkipped++;
        }
    }

    private static IEnumerable<string> EnumerateFilesSafe(string path)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
        };
        IEnumerable<string> files;
        try { files = Directory.EnumerateFiles(path, "*", options); }
        catch { yield break; }
        foreach (var f in files) yield return f;
    }
}

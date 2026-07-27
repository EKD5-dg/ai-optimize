using System.IO;
using AiOptimize.Models;
using AiOptimize.Services;

namespace AiOptimize.Tests;

public class FileCleanupHelperTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "AiOptimizeTests_" + Guid.NewGuid().ToString("N"));

    public FileCleanupHelperTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void GetDirectorySize_SumsAllFilesRecursively()
    {
        File.WriteAllBytes(Path.Combine(_dir, "a.bin"), new byte[100]);
        var sub = Directory.CreateDirectory(Path.Combine(_dir, "sub")).FullName;
        File.WriteAllBytes(Path.Combine(sub, "b.bin"), new byte[50]);

        Assert.Equal(150, FileCleanupHelper.GetDirectorySize(_dir));
    }

    [Fact]
    public void GetDirectorySize_MissingDirectory_ReturnsZero()
        => Assert.Equal(0, FileCleanupHelper.GetDirectorySize(Path.Combine(_dir, "nope")));

    [Fact]
    public void GetFilesSize_OnlyMatchesPattern()
    {
        File.WriteAllBytes(Path.Combine(_dir, "a.pf"), new byte[30]);
        File.WriteAllBytes(Path.Combine(_dir, "b.txt"), new byte[99]);

        Assert.Equal(30, FileCleanupHelper.GetFilesSize(_dir, "*.pf"));
    }

    [Fact]
    public void DeleteDirectoryContents_DeletesFilesKeepsRoot()
    {
        File.WriteAllBytes(Path.Combine(_dir, "a.bin"), new byte[100]);
        var sub = Directory.CreateDirectory(Path.Combine(_dir, "sub")).FullName;
        File.WriteAllBytes(Path.Combine(sub, "b.bin"), new byte[50]);

        var result = new CleanResult();
        FileCleanupHelper.DeleteDirectoryContents(_dir, result);

        Assert.True(Directory.Exists(_dir));
        Assert.Empty(Directory.EnumerateFileSystemEntries(_dir));
        Assert.Equal(150, result.BytesFreed);
        Assert.Equal(2, result.FilesDeleted);
        Assert.Equal(0, result.FilesSkipped);
    }

    [Fact]
    public void DeleteDirectoryContents_LockedFile_SkippedAndCounted()
    {
        var locked = Path.Combine(_dir, "locked.bin");
        File.WriteAllBytes(locked, new byte[10]);
        using var stream = new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.None);

        var result = new CleanResult();
        FileCleanupHelper.DeleteDirectoryContents(_dir, result);

        Assert.Equal(1, result.FilesSkipped);
        Assert.True(File.Exists(locked));
    }

    [Fact]
    public void DeleteFiles_OnlyDeletesMatchingPattern()
    {
        File.WriteAllBytes(Path.Combine(_dir, "a.pf"), new byte[30]);
        File.WriteAllBytes(Path.Combine(_dir, "b.txt"), new byte[99]);

        var result = new CleanResult();
        FileCleanupHelper.DeleteFiles(_dir, "*.pf", result);

        Assert.False(File.Exists(Path.Combine(_dir, "a.pf")));
        Assert.True(File.Exists(Path.Combine(_dir, "b.txt")));
        Assert.Equal(30, result.BytesFreed);
    }
}

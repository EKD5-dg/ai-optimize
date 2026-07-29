using AiOptimize.Models;
using AiOptimize.Services;

namespace AiOptimize.Tests;

public class BigFileScannerTests
{
    private const long MB = 1024 * 1024;

    [Fact]
    public void SelectTop_FiltersBelowMinAndOrdersBySizeDesc()
    {
        var files = new[]
        {
            new BigFile(@"C:\a.mp4", 500 * MB),
            new BigFile(@"C:\b.zip", 50 * MB),   // 低于阈值，应被过滤
            new BigFile(@"C:\c.iso", 2000 * MB),
            new BigFile(@"C:\d.mkv", 800 * MB),
        };

        var result = BigFileScanner.SelectTop(files, minBytes: 100 * MB, top: 10);

        Assert.Equal(new[] { @"C:\c.iso", @"C:\d.mkv", @"C:\a.mp4" }, result.Select(f => f.FullPath));
    }

    [Fact]
    public void SelectTop_TakesOnlyTopN()
    {
        var files = Enumerable.Range(1, 100).Select(i => new BigFile($@"C:\f{i}.bin", i * 200 * MB));

        var result = BigFileScanner.SelectTop(files, minBytes: 100 * MB, top: 50);

        Assert.Equal(50, result.Count);
        Assert.Equal(@"C:\f100.bin", result[0].FullPath);
    }

    [Fact]
    public void BigFile_ExposesNameAndDirectory()
    {
        var file = new BigFile(@"D:\电影\大片.mp4", 1);
        Assert.Equal("大片.mp4", file.Name);
        Assert.Equal(@"D:\电影", file.Directory);
    }
}

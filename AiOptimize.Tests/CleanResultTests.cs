using AiOptimize.Models;

namespace AiOptimize.Tests;

public class CleanResultTests
{
    [Fact]
    public void Merge_SumsCountsAndConcatsNotes()
    {
        var a = new CleanResult { BytesFreed = 100, FilesDeleted = 2, FilesSkipped = 1 };
        a.Notes.Add("n1");
        var b = new CleanResult { BytesFreed = 50, FilesDeleted = 1, FilesSkipped = 3 };
        b.Notes.Add("n2");

        a.Merge(b);

        Assert.Equal(150, a.BytesFreed);
        Assert.Equal(3, a.FilesDeleted);
        Assert.Equal(4, a.FilesSkipped);
        Assert.Equal(new[] { "n1", "n2" }, a.Notes);
    }
}

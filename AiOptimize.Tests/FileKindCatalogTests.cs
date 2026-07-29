using AiOptimize.Services;

namespace AiOptimize.Tests;

public class FileKindCatalogTests
{
    [Fact]
    public void Describe_DumpFile_IsSafeToDelete()
    {
        var info = FileKindCatalog.Describe(@"C:\Users\a\Desktop\crash.dmp");
        Assert.Equal(FileRisk.Safe, info.Risk);
        Assert.NotEmpty(info.Description);
    }

    [Fact]
    public void Describe_VirtualDiskFile_IsDanger()
    {
        var info = FileKindCatalog.Describe(@"D:\vms\data.vdi");
        Assert.Equal(FileRisk.Danger, info.Risk);
    }

    [Fact]
    public void Describe_VideoFile_IsCaution()
    {
        var info = FileKindCatalog.Describe(@"D:\电影\大片.mp4");
        Assert.Equal(FileRisk.Caution, info.Risk);
    }

    [Fact]
    public void Describe_FileInProgramFiles_UpgradedToDanger()
    {
        var info = FileKindCatalog.Describe(@"D:\Program Files\SomeApp\big.zip");
        Assert.Equal(FileRisk.Danger, info.Risk);
        Assert.Contains("软件", info.Description);
    }

    [Fact]
    public void Describe_UnknownExtension_IsCautionWithHint()
    {
        var info = FileKindCatalog.Describe(@"D:\test\unknown.xyz");
        Assert.Equal(FileRisk.Caution, info.Risk);
        Assert.NotEmpty(info.Description);
    }
}

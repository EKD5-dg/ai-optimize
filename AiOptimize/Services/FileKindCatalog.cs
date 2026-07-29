using System.IO;

namespace AiOptimize.Services;

public enum FileRisk
{
    /// <summary>垃圾/临时性质，可放心删除</summary>
    Safe,
    /// <summary>个人内容，需自行判断</summary>
    Caution,
    /// <summary>删除可能影响软件运行</summary>
    Danger,
}

public sealed record FileKindInfo(string Description, FileRisk Risk);

/// <summary>按扩展名与路径识别大文件类型，给出通俗说明与删除风险。</summary>
public static class FileKindCatalog
{
    private static readonly Dictionary<string, FileKindInfo> ByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".dmp"] = new("程序崩溃时产生的转储文件，用于排查问题，一般可以删除", FileRisk.Safe),
        [".log"] = new("日志文件，记录程序运行信息，一般可以删除", FileRisk.Safe),
        [".pml"] = new("调试跟踪记录文件，一般可以删除", FileRisk.Safe),
        [".tmp"] = new("临时文件，一般可以删除", FileRisk.Safe),
        [".bak"] = new("备份文件，确认原文件正常后可以删除", FileRisk.Caution),
        [".old"] = new("旧版本备份文件，确认不需要后可以删除", FileRisk.Caution),
        [".iso"] = new("系统/软件安装镜像，安装完成后不需要可以删除", FileRisk.Caution),
        [".zip"] = new("压缩包，确认里面的内容不再需要后可以删除", FileRisk.Caution),
        [".rar"] = new("压缩包，确认里面的内容不再需要后可以删除", FileRisk.Caution),
        [".7z"] = new("压缩包，确认里面的内容不再需要后可以删除", FileRisk.Caution),
        [".exe"] = new("程序或安装包，软件装好后安装包可以删除", FileRisk.Caution),
        [".msi"] = new("软件安装包，装好后可以删除", FileRisk.Caution),
        [".mp4"] = new("视频文件，看完不需要可以删除", FileRisk.Caution),
        [".mkv"] = new("视频文件，看完不需要可以删除", FileRisk.Caution),
        [".avi"] = new("视频文件，看完不需要可以删除", FileRisk.Caution),
        [".mov"] = new("视频文件，看完不需要可以删除", FileRisk.Caution),
        [".psd"] = new("Photoshop 设计源文件，删除后无法再编辑", FileRisk.Caution),
        [".vdi"] = new("虚拟机/安卓模拟器的磁盘文件，删除后对应软件将无法使用", FileRisk.Danger),
        [".vmdk"] = new("虚拟机的磁盘文件，删除后虚拟机将无法使用", FileRisk.Danger),
        [".vhd"] = new("虚拟磁盘文件，删除后对应功能将无法使用", FileRisk.Danger),
        [".vhdx"] = new("虚拟磁盘文件，删除后对应功能将无法使用", FileRisk.Danger),
        [".pst"] = new("Outlook 邮件数据文件，删除会丢失邮件", FileRisk.Danger),
        [".ost"] = new("Outlook 邮件缓存文件，删除前请确认邮件已同步", FileRisk.Danger),
    };

    private static readonly FileKindInfo Unknown =
        new("未知类型文件，建议先「打开位置」确认内容再决定", FileRisk.Caution);

    public static FileKindInfo Describe(string fullPath)
    {
        string extension = Path.GetExtension(fullPath);
        var info = ByExtension.TryGetValue(extension, out var known) ? known : Unknown;

        // 位于软件安装目录的文件，无论类型都提升为谨慎删除
        if (fullPath.Contains("Program Files", StringComparison.OrdinalIgnoreCase))
        {
            return new FileKindInfo(info.Description + "（位于软件安装目录，删除可能导致该软件无法使用）",
                FileRisk.Danger);
        }
        return info;
    }
}

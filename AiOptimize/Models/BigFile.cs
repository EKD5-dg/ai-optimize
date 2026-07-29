using System.IO;

namespace AiOptimize.Models;

/// <summary>大文件条目。</summary>
public sealed record BigFile(string FullPath, long Bytes)
{
    public string Name => Path.GetFileName(FullPath);
    public string Directory => Path.GetDirectoryName(FullPath) ?? "";
}

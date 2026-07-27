namespace AiOptimize.Models;

/// <summary>扫描明细项：分类名称 + 可清理字节数。</summary>
public sealed record ScanItem(string Name, long Bytes);

namespace AiOptimize.Models;

/// <summary>
/// C 盘专项清理的一个分类项。
/// Bytes 为 -1 表示无法预估大小（如 DISM 组件存储），由界面显示占位文案。
/// </summary>
public sealed record CleanupCategory(
    string Id,
    string Name,
    string Description,
    long Bytes,
    bool IsCheckedByDefault);

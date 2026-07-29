using AiOptimize.Services;

namespace AiOptimize.Models;

/// <summary>一次蓝屏事件的完整解读信息。</summary>
public sealed record BlueScreenEvent(
    DateTime Time,
    uint StopCode,
    string StopCodeText,
    string Name,
    string Cause,
    string Advice,
    string? DumpPath,
    IReadOnlyList<QuickActionType> Actions);

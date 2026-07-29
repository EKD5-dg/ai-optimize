using System.Diagnostics.Eventing.Reader;
using AiOptimize.Models;

namespace AiOptimize.Services;

/// <summary>读取系统事件日志中的蓝屏记录（BugCheck 1001）并生成解读。</summary>
public sealed class BlueScreenAnalyzer
{
    private const int MaxEvents = 50;
    private const string Query =
        "*[System[Provider[@Name='Microsoft-Windows-WER-SystemErrorReporting'] and (EventID=1001)]]";

    public Task<IReadOnlyList<BlueScreenEvent>> GetEventsAsync() => Task.Run<IReadOnlyList<BlueScreenEvent>>(() =>
    {
        var events = new List<BlueScreenEvent>();
        try
        {
            var query = new EventLogQuery("System", PathType.LogName, Query)
            {
                ReverseDirection = true, // 最新的在前
            };
            using var reader = new EventLogReader(query);
            for (EventRecord? record = reader.ReadEvent(); record is not null && events.Count < MaxEvents; record = reader.ReadEvent())
            {
                using (record)
                {
                    try
                    {
                        string message = record.FormatDescription() ?? "";
                        if (!BlueScreenMessageParser.TryParse(message, out uint code, out string? dumpPath)) continue;
                        var info = StopCodeKnowledge.Lookup(code);
                        events.Add(new BlueScreenEvent(
                            record.TimeCreated ?? DateTime.MinValue,
                            code,
                            $"0x{code:X8}",
                            info.Name,
                            info.Cause,
                            info.Advice,
                            dumpPath));
                    }
                    catch { /* 单条解析失败跳过 */ }
                }
            }
        }
        catch { /* 日志不可读时返回空列表 */ }
        return events;
    });
}

using System.Text.RegularExpressions;

namespace AiOptimize.Services;

/// <summary>从蓝屏事件（BugCheck 1001）消息文本中解析停止代码、崩溃参数与转储路径。</summary>
public static partial class BlueScreenMessageParser
{
    // 右边界断言：避免把 16 位十六进制参数的前 8 位误当停止码
    [GeneratedRegex(@"0x[0-9a-fA-F]{8}(?![0-9a-fA-F])")]
    private static partial Regex StopCodeRegex();

    [GeneratedRegex(@"[A-Za-z]:\\[^:*?""<>|\r\n。]*?\.dmp", RegexOptions.IgnoreCase)]
    private static partial Regex DumpPathRegex();

    // 括号内的十六进制参数（蓝屏参数为 64 位，最多 16 位十六进制）
    [GeneratedRegex(@"0x[0-9a-fA-F]{1,16}")]
    private static partial Regex ParamRegex();

    /// <summary>兼容旧调用：只解析停止代码与转储路径。</summary>
    public static bool TryParse(string message, out uint stopCode, out string? dumpPath)
        => TryParse(message, out stopCode, out _, out dumpPath);

    /// <summary>解析停止代码、崩溃参数（顺序同蓝屏画面，最多 4 个）与转储路径。</summary>
    public static bool TryParse(string message, out uint stopCode, out IReadOnlyList<ulong> parameters, out string? dumpPath)
    {
        stopCode = 0;
        parameters = Array.Empty<ulong>();
        dumpPath = null;
        if (string.IsNullOrEmpty(message)) return false;

        var codeMatch = StopCodeRegex().Match(message);
        if (!codeMatch.Success) return false;
        stopCode = Convert.ToUInt32(codeMatch.Value, 16);

        // 参数紧跟在停止代码后的括号里：0x3B (0xc0000005, 0xfffff800..., ...)
        int open = message.IndexOf('(', codeMatch.Index + codeMatch.Length);
        if (open > 0)
        {
            int close = message.IndexOf(')', open);
            if (close > open)
            {
                string segment = message[open..(close + 1)];
                var list = new List<ulong>(4);
                foreach (Match m in ParamRegex().Matches(segment))
                {
                    if (list.Count >= 4) break;
                    list.Add(Convert.ToUInt64(m.Value, 16));
                }
                if (list.Count > 0) parameters = list;
            }
        }

        var dumpMatch = DumpPathRegex().Match(message);
        if (dumpMatch.Success) dumpPath = dumpMatch.Value;
        return true;
    }
}

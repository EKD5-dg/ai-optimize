using System.Text.RegularExpressions;

namespace AiOptimize.Services;

/// <summary>从蓝屏事件（BugCheck 1001）消息文本中解析停止代码与转储路径。</summary>
public static partial class BlueScreenMessageParser
{
    // 右边界断言：避免把 16 位十六进制参数的前 8 位误当停止码
    [GeneratedRegex(@"0x[0-9a-fA-F]{8}(?![0-9a-fA-F])")]
    private static partial Regex StopCodeRegex();

    [GeneratedRegex(@"[A-Za-z]:\\[^:*?""<>|\r\n。]*?\.dmp", RegexOptions.IgnoreCase)]
    private static partial Regex DumpPathRegex();

    public static bool TryParse(string message, out uint stopCode, out string? dumpPath)
    {
        stopCode = 0;
        dumpPath = null;
        if (string.IsNullOrEmpty(message)) return false;

        var codeMatch = StopCodeRegex().Match(message);
        if (!codeMatch.Success) return false;
        stopCode = Convert.ToUInt32(codeMatch.Value, 16);

        var dumpMatch = DumpPathRegex().Match(message);
        if (dumpMatch.Success) dumpPath = dumpMatch.Value;
        return true;
    }
}

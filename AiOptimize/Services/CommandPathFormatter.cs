namespace AiOptimize.Services;

/// <summary>启动命令展示净化：去掉引号与启动参数，仅保留程序路径。</summary>
public static class CommandPathFormatter
{
    public static string Clean(string? command)
    {
        if (string.IsNullOrWhiteSpace(command)) return "";
        string text = command.Trim();

        if (text.StartsWith('"'))
        {
            int closing = text.IndexOf('"', 1);
            return closing > 0 ? text[1..closing] : text.Trim('"');
        }

        // 无引号：按 .exe 边界截断参数
        int exeEnd = text.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exeEnd > 0) return text[..(exeEnd + 4)];
        return text;
    }
}

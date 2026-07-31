namespace AiOptimize.Services;

/// <summary>启动项搜索匹配：按名称/说明/路径模糊匹配，忽略大小写。</summary>
public static class StartupSearch
{
    public static bool Matches(string name, string description, string command, string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        string q = query.Trim();
        return name.Contains(q, StringComparison.OrdinalIgnoreCase)
            || description.Contains(q, StringComparison.OrdinalIgnoreCase)
            || command.Contains(q, StringComparison.OrdinalIgnoreCase);
    }
}

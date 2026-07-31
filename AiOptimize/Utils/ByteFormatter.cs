namespace AiOptimize.Utils;

public static class ByteFormatter
{
    public static string Format(long bytes) => Format((double)bytes);

    public static string Format(ulong bytes) => Format((double)bytes);

    public static string Format(double bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        // 负数按绝对值分级后补回符号（如配额环境下 FreeBytes 可能为负）
        string sign = bytes < 0 ? "-" : "";
        double value = Math.Abs(bytes);
        int i = 0;
        while (value >= 1024 && i < units.Length - 1) { value /= 1024; i++; }
        return $"{sign}{value:0.#} {units[i]}";
    }
}

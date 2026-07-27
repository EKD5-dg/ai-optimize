namespace AiOptimize.Utils;

public static class ByteFormatter
{
    public static string Format(long bytes) => Format((double)bytes);

    public static string Format(double bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        while (bytes >= 1024 && i < units.Length - 1) { bytes /= 1024; i++; }
        return $"{bytes:0.#} {units[i]}";
    }
}

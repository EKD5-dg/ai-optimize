namespace AiOptimize.Services;

/// <summary>任务管理器 StartupApproved 二进制值约定：首字节偶数=启用，奇数=禁用；无数据视为启用。</summary>
public static class StartupApprovedState
{
    public static bool IsEnabled(byte[]? data)
        => data is null || data.Length == 0 || (data[0] & 0x01) == 0;

    public static byte[] CreateEnabledValue()
    {
        var data = new byte[12];
        data[0] = 0x02;
        return data;
    }

    public static byte[] CreateDisabledValue()
    {
        var data = new byte[12];
        data[0] = 0x03;
        BitConverter.GetBytes(DateTime.Now.ToFileTime()).CopyTo(data, 4);
        return data;
    }
}

namespace AiOptimize.Services;

public enum StartupAdvice
{
    /// <summary>系统/安全相关，建议保留</summary>
    Keep,
    /// <summary>常见软件，可按需关闭</summary>
    Optional,
    /// <summary>未识别，自行判断</summary>
    Unknown,
}

public sealed record StartupInfo(string Description, StartupAdvice Advice);

/// <summary>常见启动项知识库：按名称/路径关键词识别，给出大白话说明与建议。</summary>
public static class StartupKnowledge
{
    private sealed record Entry(string[] Keywords, string Description, StartupAdvice Advice);

    private static readonly Entry[] Entries =
    {
        // 系统/输入相关：建议保留
        new(new[] { "ctfmon" }, "Windows 输入法服务，关闭可能导致打不出字", StartupAdvice.Keep),
        new(new[] { "SecurityHealth" }, "Windows 安全中心，保护电脑安全", StartupAdvice.Keep),
        new(new[] { "OneDrive" }, "微软 OneDrive 网盘同步，使用网盘同步文件则保留", StartupAdvice.Optional),
        new(new[] { "Huorong", "HipsTray" }, "火绒安全软件，建议保留", StartupAdvice.Keep),
        new(new[] { "360Tray", "360Safe" }, "360 安全卫士，建议保留（除非你不想用它）", StartupAdvice.Keep),
        new(new[] { "Realtek", "RtkAudio", "RAVCpl" }, "声卡驱动程序，建议保留", StartupAdvice.Keep),
        new(new[] { "NVIDIA", "nvcontainer" }, "显卡驱动服务，建议保留", StartupAdvice.Keep),
        new(new[] { "igfx", "IntelGraphics" }, "Intel 显卡服务，建议保留", StartupAdvice.Keep),

        // 常见软件：按需关闭
        new(new[] { "WeChat", "Weixin" }, "微信，关闭后开机不自动登录，手动打开即可", StartupAdvice.Optional),
        new(new[] { "WXWork" }, "企业微信，不用它办公可以关闭", StartupAdvice.Optional),
        new(new[] { "DingTalk", "DingDing", "Dingtalk" }, "钉钉，不用它办公可以关闭", StartupAdvice.Optional),
        new(new[] { "QQNT", @"Tencent\QQ", "QQProtect" }, "QQ，关闭后需要时手动打开即可", StartupAdvice.Optional),
        new(new[] { "Feishu", "Lark" }, "飞书，不用它办公可以关闭", StartupAdvice.Optional),
        new(new[] { "CloudMusic", "QQMusic", "KuGou", "KwService" }, "音乐软件，可以关闭，听歌时手动打开", StartupAdvice.Optional),
        new(new[] { "iQIYI", "QQLive", "Youku" }, "视频软件，可以关闭，看视频时手动打开", StartupAdvice.Optional),
        new(new[] { "BaiduNetdisk" }, "百度网盘，可以关闭，传文件时手动打开", StartupAdvice.Optional),
        new(new[] { "aDrive", "AliyunDrive" }, "阿里云盘，可以关闭，传文件时手动打开", StartupAdvice.Optional),
        new(new[] { "Thunder", "XLLiveUD" }, "迅雷下载，可以关闭，下载时手动打开", StartupAdvice.Optional),
        new(new[] { "Steam" }, "Steam 游戏平台，可以关闭，玩游戏时手动打开", StartupAdvice.Optional),
        new(new[] { "WeGame" }, "WeGame 游戏平台，可以关闭", StartupAdvice.Optional),
        new(new[] { "JetBrains", "Toolbox" }, "JetBrains 开发工具管理器，不写代码可以关闭", StartupAdvice.Optional),
        new(new[] { "Everything" }, "Everything 文件搜索工具，常用搜索则保留", StartupAdvice.Optional),
        new(new[] { "Snipaste", "PixPin" }, "截图工具，常用截图则保留", StartupAdvice.Optional),
        new(new[] { "TodeskService", "ToDesk", "SunloginClient" }, "远程控制软件，需要远程时保留", StartupAdvice.Optional),
        new(new[] { "MuMu", "Netease" }, "网易 MuMu 模拟器相关服务，不用模拟器可以关闭", StartupAdvice.Optional),
        new(new[] { "AutoClaw" }, "AutoClaw 工具，确认自己在用则保留", StartupAdvice.Optional),
        new(new[] { "LongiVPN", "EnUOE" }, "公司网络/VPN 客户端，办公需要则保留", StartupAdvice.Keep),
    };

    private static readonly StartupInfo Fallback =
        new("未识别的程序。若是你常用的软件建议保留；不确定可先关闭观察，有问题再打开", StartupAdvice.Unknown);

    public static StartupInfo Describe(string name, string command)
    {
        foreach (var entry in Entries)
        {
            foreach (var keyword in entry.Keywords)
            {
                if (name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    command.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return new StartupInfo(entry.Description, entry.Advice);
            }
        }
        return Fallback;
    }
}

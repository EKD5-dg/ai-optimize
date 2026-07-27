namespace AiOptimize.Models;

public enum StartupSource
{
    HkcuRun,
    HklmRun,
    HklmRunWow64,
    UserStartupFolder,
    CommonStartupFolder,
}

public sealed class StartupItem
{
    public required string Name { get; init; }
    public required string Command { get; init; }
    public required StartupSource Source { get; init; }
    public bool IsEnabled { get; set; }

    public string SourceDisplay => Source switch
    {
        StartupSource.HkcuRun => "注册表（当前用户）",
        StartupSource.HklmRun => "注册表（所有用户）",
        StartupSource.HklmRunWow64 => "注册表（所有用户 32 位）",
        StartupSource.UserStartupFolder => "启动文件夹（当前用户）",
        StartupSource.CommonStartupFolder => "启动文件夹（所有用户）",
        _ => "未知",
    };
}

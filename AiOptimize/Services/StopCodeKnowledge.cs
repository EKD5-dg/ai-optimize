namespace AiOptimize.Services;

public sealed record StopCodeInfo(string Name, string Cause, string Advice);

/// <summary>常见蓝屏停止代码知识库：翻译为通俗中文原因与排查建议。</summary>
public static class StopCodeKnowledge
{
    private static readonly StopCodeInfo Generic = new(
        "未知错误",
        "该错误代码不在常见列表中，可能由驱动、硬件或系统文件损坏引起。",
        "更新显卡/存储等驱动与 Windows 补丁；运行 Windows 内存诊断；在命令提示符执行 sfc /scannow 检查系统文件。");

    private static readonly Dictionary<uint, StopCodeInfo> Map = new()
    {
        [0x0A] = new("IRQL_NOT_LESS_OR_EQUAL", "驱动程序访问了无效的内存地址，多为驱动缺陷。", "回滚或更新最近安装的驱动程序，特别是显卡、网卡驱动。"),
        [0x1A] = new("MEMORY_MANAGEMENT", "内存管理出错，常见于内存条故障或驱动损坏内存。", "运行 Windows 内存诊断（搜索\"内存诊断\"），必要时重新插拔或更换内存条。"),
        [0x1E] = new("KMODE_EXCEPTION_NOT_HANDLED", "内核模式程序产生了未处理的异常，多为驱动问题。", "记下蓝屏页面提到的文件名，更新对应驱动；排查最近安装的软件。"),
        [0x24] = new("NTFS_FILE_SYSTEM", "NTFS 文件系统驱动出错，常见于磁盘坏道或文件系统损坏。", "对系统盘运行 chkdsk /f /r 检查修复磁盘；关注硬盘健康状态。"),
        [0x34] = new("CACHE_MANAGER", "系统缓存管理器出错，通常与磁盘故障、存储驱动或内存有关。", "检查磁盘健康（chkdsk /f /r）；更新存储控制器驱动；运行内存诊断。"),
        [0x3B] = new("SYSTEM_SERVICE_EXCEPTION", "系统服务执行时发生异常，常见于驱动或系统文件损坏。", "更新驱动；执行 sfc /scannow；排查最近安装的安全类软件。"),
        [0x50] = new("PAGE_FAULT_IN_NONPAGED_AREA", "系统访问了不存在的内存页，常见于内存故障或驱动缺陷。", "运行内存诊断；卸载最近安装的驱动或软件；检查磁盘。"),
        [0x7A] = new("KERNEL_DATA_INPAGE_ERROR", "从磁盘读取系统数据失败，多为磁盘坏道或数据线接触不良。", "运行 chkdsk /f /r；检查硬盘连接线；关注硬盘 SMART 健康状态。"),
        [0x7B] = new("INACCESSIBLE_BOOT_DEVICE", "系统启动时找不到引导磁盘，常见于更换硬件或存储驱动问题。", "检查 BIOS 硬盘模式设置（AHCI/RAID）；恢复最近的硬件改动。"),
        [0x7E] = new("SYSTEM_THREAD_EXCEPTION_NOT_HANDLED", "系统线程产生未处理异常，多为驱动缺陷。", "查看蓝屏页面提到的驱动文件名并更新；更新 BIOS。"),
        [0x7F] = new("UNEXPECTED_KERNEL_MODE_TRAP", "内核意外陷入错误，常见于硬件故障或超频。", "取消超频；运行内存诊断；检查散热。"),
        [0x9F] = new("DRIVER_POWER_STATE_FAILURE", "驱动在睡眠/唤醒时未正确响应电源状态切换。", "更新显卡、网卡与芯片组驱动；在设备管理器排查异常设备。"),
        [0xC2] = new("BAD_POOL_CALLER", "程序错误地使用了系统内存池，多为驱动或安全软件问题。", "卸载最近安装的驱动/安全软件；更新系统补丁。"),
        [0xD1] = new("DRIVER_IRQL_NOT_LESS_OR_EQUAL", "驱动程序访问了无效内存，典型的驱动缺陷。", "更新或回滚网卡、显卡等最近变动过的驱动。"),
        [0xEF] = new("CRITICAL_PROCESS_DIED", "系统关键进程意外终止，常见于系统文件损坏或磁盘故障。", "执行 sfc /scannow 与 DISM /Online /Cleanup-Image /RestoreHealth；检查磁盘。"),
        [0xF4] = new("CRITICAL_OBJECT_TERMINATION", "关键系统对象被终止，常见于磁盘故障或系统文件损坏。", "检查硬盘健康；执行 sfc /scannow；排查安全软件。"),
        [0x116] = new("VIDEO_TDR_FAILURE", "显卡驱动长时间无响应被系统重置。", "干净重装显卡驱动（可用 DDU 卸载后装新版）；检查显卡温度。"),
        [0x124] = new("WHEA_UNCORRECTABLE_ERROR", "硬件报告了无法纠正的错误，常见于 CPU/主板/电源问题或超频。", "取消超频；检查散热与电源；更新 BIOS；必要时逐件排查硬件。"),
        [0x133] = new("DPC_WATCHDOG_VIOLATION", "驱动响应超时，常见于固态硬盘固件或驱动过旧。", "更新固态硬盘固件与存储驱动；更新芯片组驱动。"),
        [0x139] = new("KERNEL_SECURITY_CHECK_FAILURE", "内核安全检查失败，常见于驱动缺陷或内存故障。", "更新驱动；运行内存诊断；执行 sfc /scannow。"),
    };

    public static StopCodeInfo Lookup(uint stopCode)
        => Map.TryGetValue(stopCode, out var info) ? info : Generic;
}

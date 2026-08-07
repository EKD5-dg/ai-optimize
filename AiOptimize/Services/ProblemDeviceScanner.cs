using System.Management;
using AiOptimize.Models;

namespace AiOptimize.Services;

/// <summary>扫描有问题的硬件设备（WMI 查询 Win32_PnPEntity）。</summary>
public static class ProblemDeviceScanner
{
    /// <summary>获取所有状态非正常的设备。</summary>
    public static IReadOnlyList<ProblemDevice> Scan()
    {
        var list = new List<ProblemDevice>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, DeviceID, Status, ConfigManagerErrorCode FROM Win32_PnPEntity WHERE Status != 'OK'");
            foreach (var obj in searcher.Get())
            {
                using (obj)
                {
                    string name = obj["Name"]?.ToString() ?? "未知设备";
                    string deviceId = obj["DeviceID"]?.ToString() ?? "";
                    string status = obj["Status"]?.ToString() ?? "未知";
                    int errorCode = obj["ConfigManagerErrorCode"] is int code ? code : -1;
                    string problem = DescribeProblem(errorCode, status);
                    list.Add(new ProblemDevice(name, deviceId, status, problem));
                }
            }
        }
        catch
        {
            // WMI 查询失败时返回空列表
        }
        return list;
    }

    private static string DescribeProblem(int errorCode, string status) => errorCode switch
    {
        1 => "设备未正确配置",
        3 => "驱动程序损坏或版本不兼容",
        10 => "设备无法启动",
        12 => "资源冲突",
        22 => "设备已被禁用",
        24 => "设备不存在或驱动未安装",
        28 => "驱动程序未安装",
        29 => "设备固件未提供所需资源",
        31 => "驱动程序加载失败",
        32 => "驱动程序服务已被禁用",
        37 => "驱动程序返回错误",
        39 => "驱动程序损坏或丢失",
        41 => "驱动程序加载失败（硬件不存在）",
        43 => "驱动程序报告设备故障",
        45 => "设备已断开连接",
        47 => "设备被安全移除",
        48 => "驱动程序被阻止加载",
        49 => "设备堆栈损坏",
        50 => "设备无法启动（资源不足）",
        51 => "设备等待其他设备启动",
        52 => "驱动程序签名问题",
        _ => $"设备状态异常（{status}）",
    };
}

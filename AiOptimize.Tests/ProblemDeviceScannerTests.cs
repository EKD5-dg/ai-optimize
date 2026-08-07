using AiOptimize.Models;
using AiOptimize.Services;

namespace AiOptimize.Tests;

public class ProblemDeviceScannerTests
{
    [Fact]
    public void Scan_ReturnsList()
    {
        // 在开发机上可能没有问题设备，也可能有，只验证不抛异常
        var result = ProblemDeviceScanner.Scan();
        Assert.NotNull(result);
    }

    [Fact]
    public void ProblemDevice_RecordProperties()
    {
        var device = new ProblemDevice("测试设备", "USB\\VID_1234", "Error", "驱动未安装");
        Assert.Equal("测试设备", device.Name);
        Assert.Equal("USB\\VID_1234", device.DeviceId);
        Assert.Equal("Error", device.Status);
        Assert.Equal("驱动未安装", device.ProblemDescription);
    }
}

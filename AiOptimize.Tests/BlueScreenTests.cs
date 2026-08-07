using AiOptimize.Services;

namespace AiOptimize.Tests;

public class BlueScreenParserTests
{
    private const string RealMessage =
        "计算机已经从检测错误后重新启动。检测错误: 0x00000034 (0x000000000000029d, 0xffffffffc0000420, 0x0000000000000000, 0x0000000000000000)。已将转储保存在: C:\\Windows\\MEMORY.DMP。报告 ID: abc123。";

    [Fact]
    public void TryParse_RealChineseMessage_ExtractsCodeAndDump()
    {
        var ok = BlueScreenMessageParser.TryParse(RealMessage, out uint code, out string? dumpPath);

        Assert.True(ok);
        Assert.Equal(0x34u, code);
        Assert.Equal("C:\\Windows\\MEMORY.DMP", dumpPath);
    }

    [Fact]
    public void TryParse_MessageWithoutDumpPath_CodeOnly()
    {
        var ok = BlueScreenMessageParser.TryParse("检测错误: 0x0000007E (0x0, 0x0, 0x0, 0x0)。", out uint code, out string? dumpPath);

        Assert.True(ok);
        Assert.Equal(0x7Eu, code);
        Assert.Null(dumpPath);
    }

    [Fact]
    public void TryParse_NoStopCode_ReturnsFalse()
        => Assert.False(BlueScreenMessageParser.TryParse("没有代码的消息", out _, out _));
}

public class StopCodeKnowledgeTests
{
    [Fact]
    public void Lookup_KnownCode_ReturnsSpecificEntry()
    {
        var info = StopCodeKnowledge.Lookup(0x34);

        Assert.Equal("CACHE_MANAGER", info.Name);
        Assert.NotEmpty(info.Cause);
        Assert.NotEmpty(info.Advice);
    }

    [Fact]
    public void Lookup_UnknownCode_ReturnsGenericEntry()
    {
        var info = StopCodeKnowledge.Lookup(0xDEADBEEF);

        Assert.Equal("未知错误", info.Name);
        Assert.NotEmpty(info.Advice);
    }

    [Theory]
    [InlineData(0x18, "REFERENCE_BY_POINTER")]
    [InlineData(0x19, "BAD_POOL_HEADER")]
    [InlineData(0x4E, "PFN_LIST_CORRUPT")]
    [InlineData(0x51, "REGISTRY_ERROR")]
    [InlineData(0x74, "BAD_SYSTEM_CONFIG_INFO")]
    [InlineData(0x7C, "BUGCODE_NDIS_DRIVER")]
    [InlineData(0x8E, "KERNEL_MODE_EXCEPTION_NOT_HANDLED")]
    [InlineData(0x9C, "MACHINE_CHECK_EXCEPTION")]
    [InlineData(0xA5, "ACPI_BIOS_ERROR")]
    [InlineData(0xBE, "ATTEMPTED_WRITE_TO_READONLY_MEMORY")]
    [InlineData(0xC4, "DRIVER_VERIFIER_DETECTED_VIOLATION")]
    [InlineData(0xEA, "THREAD_STUCK_IN_DEVICE_DRIVER")]
    [InlineData(0xED, "UNMOUNTABLE_BOOT_VOLUME")]
    [InlineData(0xFE, "BUGCODE_USB_DRIVER")]
    [InlineData(0x101, "CLOCK_WATCHDOG_TIMEOUT")]
    [InlineData(0x109, "CRITICAL_STRUCTURE_CORRUPTION")]
    [InlineData(0x10E, "VIDEO_MEMORY_MANAGEMENT_INTERNAL")]
    [InlineData(0x117, "VIDEO_TDR_TIMEOUT_DETECTED")]
    [InlineData(0x12B, "FAULTY_HARDWARE_CORRUPTED_PAGE")]
    [InlineData(0x13A, "KERNEL_MODE_HEAP_CORRUPTION")]
    public void Lookup_NewlyAddedCodes_ReturnsSpecificEntries(uint code, string expectedName)
    {
        var info = StopCodeKnowledge.Lookup(code);

        Assert.Equal(expectedName, info.Name);
        Assert.NotEmpty(info.Cause);
        Assert.NotEmpty(info.Advice);
        Assert.NotEmpty(info.Actions);
    }
}

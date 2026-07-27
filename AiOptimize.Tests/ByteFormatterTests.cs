using AiOptimize.Utils;

namespace AiOptimize.Tests;

public class ByteFormatterTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1073741824, "1 GB")]
    public void Format_ReturnsHumanReadable(long bytes, string expected)
        => Assert.Equal(expected, ByteFormatter.Format(bytes));
}

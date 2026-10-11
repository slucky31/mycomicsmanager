using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class FileSizeFormatterTests
{
    [Theory]
    [InlineData(0L, "0 B")]
    [InlineData(1023L, "1023 B")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(5L * 1024 * 1024 * 1024, "5 GB")]
    [InlineData(2L * 1024 * 1024 * 1024 * 1024 * 1024, "2048 TB")]
    public void Format_Should_UseLargestUnit_WhenBytesGiven(long bytes, string expected)
    {
        FileSizeFormatter.Format(bytes).Should().Be(expected);
    }
}

using AwesomeAssertions;
using Web.Infrastructure;
using Xunit;

namespace Web.Tests.Infrastructure;

public sealed class AppVersionTests
{
    [Theory]
    [InlineData("10.25.1+17db9b2a4c", "10.25.1")]
    [InlineData("10.25.1", "10.25.1")]
    public void Format_Should_ReturnReleaseNumber_WhenInformationalVersionIsSet(string informationalVersion, string expected)
    {
        AppVersion.Format(informationalVersion, new Version(1, 0, 0, 0)).Should().Be(expected);
    }

    [Fact]
    public void Format_Should_FallBackToAssemblyVersion_WhenInformationalVersionIsMissing()
    {
        AppVersion.Format(null, new Version(10, 25, 1, 0)).Should().Be("10.25.1");
    }

    [Fact]
    public void Format_Should_ReturnUnknown_WhenNoVersionIsAvailable()
    {
        AppVersion.Format(" ", null).Should().Be("unknown");
    }
}

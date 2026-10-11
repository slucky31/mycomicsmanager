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

    [Theory]
    [InlineData("mcm.Web.styles.css", "10.32.0", "mcm.Web.styles.css?v=10.32.0")]
    [InlineData("./js/bookReader.js", "10.32.0-beta+1", "./js/bookReader.js?v=10.32.0-beta%2B1")]
    public void Versioned_Should_AppendTheEscapedVersionToThePath(string path, string version, string expected)
    {
        AppVersion.Versioned(path, version).Should().Be(expected);
    }

    [Fact]
    public void Versioned_Should_UseTheCurrentVersion()
    {
        AppVersion.Versioned("mcm.Web.styles.css").Should().Be($"mcm.Web.styles.css?v={Uri.EscapeDataString(AppVersion.Current)}");
    }

    [Fact]
    public void Versioned_Should_Throw_WhenThePathIsEmpty()
    {
        var act = () => AppVersion.Versioned(" ", "10.32.0");

        act.Should().Throw<ArgumentException>();
    }
}

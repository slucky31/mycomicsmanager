using Domain.Extensions;

namespace Domain.UnitTests.Extensions;

public class UriExtensionTests
{
    private static readonly HashSet<string> s_allowedHosts = new(["miniflux"], StringComparer.OrdinalIgnoreCase);

    [Theory]
    [InlineData("https://miniflux/v1/entries", false, true)]
    [InlineData("http://miniflux:8080/v1/entries", false, false)]
    [InlineData("http://miniflux:8080/v1/entries", true, true)]
    [InlineData("http://evil.internal/v1/entries", true, false)]
    [InlineData("ftp://miniflux/file", true, false)]
    public void IsAllowedHost_Should_CheckSchemeAndHost(string address, bool allowHttp, bool expected)
    {
        new Uri(address).IsAllowedHost(s_allowedHosts, allowHttp).Should().Be(expected);
    }

    [Fact]
    public void IsAllowedHost_Should_ReturnFalse_WhenUriIsNull()
    {
        ((Uri?)null).IsAllowedHost(s_allowedHosts, allowHttp: true).Should().BeFalse();
    }

    [Theory]
    [InlineData("https://cdn.miniflux/file", false, false)]
    [InlineData("https://cdn.miniflux/file", true, true)]
    public void IsAllowedHost_Should_AcceptSubdomains_OnlyWhenAllowed(string address, bool allowSubdomains, bool expected)
    {
        new Uri(address).IsAllowedHost(s_allowedHosts, allowHttp: false, allowSubdomains).Should().Be(expected);
    }
}

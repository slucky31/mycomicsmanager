using Domain.Extensions;

namespace Domain.UnitTests.Extensions;

public class HostExtensionTests
{
    private static readonly string[] s_domains = ["1fichier.com", "rapidgator.net"];

    [Theory]
    [InlineData("1fichier.com", true)]
    [InlineData("CDN.1fichier.com", true)]
    [InlineData("a.b.rapidgator.net", true)]
    [InlineData("evil1fichier.com", false)]
    [InlineData("1fichier.com.evil.org", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsSameOrSubdomainOf_Should_MatchDomainAndItsSubdomainsOnly(string? host, bool expected)
    {
        host.IsSameOrSubdomainOf(s_domains).Should().Be(expected);
    }
}

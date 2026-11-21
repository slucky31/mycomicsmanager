using Application.FeedImports;
using AwesomeAssertions;
using Web.Infrastructure;
using Xunit;

namespace Web.Tests.Infrastructure;

public sealed class DebridLinkHostCacheTests
{
    private sealed class MovableTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 21, 23, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public void Get_Should_ReturnTheHosts_UntilTheCacheDurationIsOver()
    {
        var time = new MovableTimeProvider();
        var cache = new DebridLinkHostCache(time);
        IReadOnlyList<DebridLinkHost> hosts = [new DebridLinkHost("ddl", false, ["ddl.to"])];

        cache.Get().Should().BeNull();
        cache.Set(hosts);
        time.Now += DebridLinkHostCache.CacheDuration - TimeSpan.FromSeconds(1);
        cache.Get().Should().BeSameAs(hosts);
        time.Now += TimeSpan.FromSeconds(1);
        cache.Get().Should().BeNull();
    }
}

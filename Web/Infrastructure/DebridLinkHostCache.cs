using Application.FeedImports;

namespace Web.Infrastructure;

// The Debrid-Link host states are read by the Feeds page, the sync and every download: kept a few minutes so a
// page refresh or a burst of downloads makes one call. Singleton, as the typed HttpClient is transient.
internal sealed class DebridLinkHostCache(TimeProvider timeProvider)
{
    public static TimeSpan CacheDuration => TimeSpan.FromMinutes(5);

    // ponytail: plain fields, no lock; a concurrent miss makes two calls, harmless at this traffic level.
    private IReadOnlyList<DebridLinkHost>? _hosts;
    private DateTimeOffset _cachedAt;

    public IReadOnlyList<DebridLinkHost>? Get() =>
        _hosts is { } hosts && timeProvider.GetUtcNow() - _cachedAt < CacheDuration ? hosts : null;

    public void Set(IReadOnlyList<DebridLinkHost> hosts)
    {
        _hosts = hosts;
        _cachedAt = timeProvider.GetUtcNow();
    }
}

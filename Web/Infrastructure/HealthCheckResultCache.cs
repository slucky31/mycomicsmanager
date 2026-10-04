using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Web.Infrastructure;

// /health is public and unauthenticated (needed for the Docker HEALTHCHECK and infra probes),
// so without caching every hit - and every Settings page load - would fire a real third-party
// API call with no rate limit, i.e. a free trigger for API cost or quota. The cache is a
// singleton per health check type (see Program.cs) because the checks depend on scoped or
// transient services (ICloudinaryService, typed HttpClients), so they can't be singletons
// themselves without a captive-dependency DI error.
#pragma warning disable S2326 // TCheck only keys one singleton cache per health check type
internal sealed class HealthCheckResultCache<TCheck> where TCheck : IHealthCheck
#pragma warning restore S2326
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    // ponytail: plain fields, no lock; a concurrent cache miss can fire two checks at once,
    // fine at this traffic level - add a lock/SemaphoreSlim if that changes.
    private HealthCheckResult? _cachedResult;
    private DateTimeOffset _cachedAt;

    public async Task<HealthCheckResult> GetOrAddAsync(Func<CancellationToken, Task<HealthCheckResult>> check, CancellationToken cancellationToken)
    {
        if (_cachedResult is { } cached && DateTimeOffset.UtcNow - _cachedAt < CacheDuration)
        {
            return cached;
        }

        var result = await check(cancellationToken);
        _cachedResult = result;
        _cachedAt = DateTimeOffset.UtcNow;
        return result;
    }
}

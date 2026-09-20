using Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Web.Infrastructure;

// /health is public and unauthenticated (needed for the Docker HEALTHCHECK and infra probes),
// so without caching every hit - and every Settings page load - would fire a real Cloudinary
// API call with no rate limit, i.e. a free trigger for third-party API cost. The cache lives in
// its own singleton (see Program.cs) because ICloudinaryService is scoped, so the health check
// itself can't be a singleton without a captive-dependency DI error.
internal sealed class CloudinaryHealthCheckCache
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    // ponytail: plain fields, no lock; a concurrent cache miss can fire two pings at once,
    // fine at this traffic level - add a lock/SemaphoreSlim if that changes.
    private HealthCheckResult? _cachedResult;
    private DateTimeOffset _cachedAt;

    public bool TryGet(out HealthCheckResult result)
    {
        if (_cachedResult is { } cached && DateTimeOffset.UtcNow - _cachedAt < CacheDuration)
        {
            result = cached;
            return true;
        }

        result = default;
        return false;
    }

    public void Set(HealthCheckResult result)
    {
        _cachedResult = result;
        _cachedAt = DateTimeOffset.UtcNow;
    }
}

internal sealed class CloudinaryHealthCheck(ICloudinaryService cloudinaryService, CloudinaryHealthCheckCache cache) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (cache.TryGet(out var cached))
        {
            return cached;
        }

        var reachable = await cloudinaryService.PingAsync(cancellationToken);
        var result = reachable
            ? HealthCheckResult.Healthy("Cloudinary account is reachable.")
            : HealthCheckResult.Unhealthy("Cloudinary account is not reachable.");

        cache.Set(result);
        return result;
    }
}

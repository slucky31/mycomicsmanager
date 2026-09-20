using Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Web.Infrastructure;

// Registered as a singleton (see Program.cs) so this cache is actually shared between calls.
// /health is public and unauthenticated (needed for the Docker HEALTHCHECK and infra probes),
// so without caching every hit - and every Settings page load - would fire a real Cloudinary
// API call with no rate limit, i.e. a free trigger for third-party API cost.
internal sealed class CloudinaryHealthCheck(ICloudinaryService cloudinaryService) : IHealthCheck
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    // ponytail: plain fields, no lock; a concurrent cache miss can fire two pings at once on a
    // single instance, fine at this traffic level - add a lock/SemaphoreSlim if that changes.
    private HealthCheckResult? _cachedResult;
    private DateTimeOffset _cachedAt;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_cachedResult is { } cached && DateTimeOffset.UtcNow - _cachedAt < CacheDuration)
        {
            return cached;
        }

        var reachable = await cloudinaryService.PingAsync(cancellationToken);
        var result = reachable
            ? HealthCheckResult.Healthy("Cloudinary account is reachable.")
            : HealthCheckResult.Unhealthy("Cloudinary account is not reachable.");

        _cachedResult = result;
        _cachedAt = DateTimeOffset.UtcNow;
        return result;
    }
}

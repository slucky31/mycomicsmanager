using Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Web.Infrastructure;

internal sealed class CloudinaryHealthCheck(ICloudinaryService cloudinaryService, HealthCheckResultCache<CloudinaryHealthCheck> cache) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        cache.GetOrAddAsync(async ct =>
        {
            var reachable = await cloudinaryService.PingAsync(ct);
            return reachable
                ? HealthCheckResult.Healthy("Cloudinary account is reachable.")
                : HealthCheckResult.Unhealthy("Cloudinary account is not reachable.");
        }, cancellationToken);
}

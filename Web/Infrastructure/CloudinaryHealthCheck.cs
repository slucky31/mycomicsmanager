using Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Web.Infrastructure;

internal sealed class CloudinaryHealthCheck(ICloudinaryService cloudinaryService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var reachable = await cloudinaryService.PingAsync(cancellationToken);
        return reachable
            ? HealthCheckResult.Healthy("Cloudinary account is reachable.")
            : HealthCheckResult.Unhealthy("Cloudinary account is not reachable.");
    }
}

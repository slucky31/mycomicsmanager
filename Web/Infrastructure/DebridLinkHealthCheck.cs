using Application.FeedImports;
using Application.Interfaces;
using Domain.Settings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Web.Infrastructure;

// Debrid-Link only serves feed import downloads: a failure is Degraded, never Unhealthy, so the
// rest of the application keeps running (and starting) without it.
internal sealed class DebridLinkHealthCheck(
    IDebridLinkClient debridLinkClient,
    IFeatureToggles featureToggles,
    IOptions<DebridLinkSettings> debridLinkSettings,
    HealthCheckResultCache<DebridLinkHealthCheck> cache) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!featureToggles.IsEnabled(FeatureToggle.FeedImport))
        {
            return Task.FromResult(HealthCheckResult.Healthy("Feed import is turned off: Debrid-Link is not checked."));
        }

        if (string.IsNullOrWhiteSpace(debridLinkSettings.Value.ApiKey))
        {
            return Task.FromResult(HealthCheckResult.Degraded("Debrid-Link API key is not configured (DebridLink:ApiKey): downloads are not started."));
        }

        return cache.GetOrAddAsync(async ct =>
        {
            var account = await debridLinkClient.CheckAccountAsync(ct);
            return account.IsSuccess
                ? HealthCheckResult.Healthy("Debrid-Link API is reachable and the API key is valid.")
                : HealthCheckResult.Degraded($"Debrid-Link check failed: {account.Error!.Description}");
        }, cancellationToken);
    }
}

using Application.FeedImports;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Web.Infrastructure;

// Miniflux only feeds the background sync: a failure is Degraded, never Unhealthy, so the rest
// of the application keeps running (and starting) without it.
internal sealed class MinifluxHealthCheck(
    IMinifluxClient minifluxClient,
    IOptions<FeedImportSettings> feedImportSettings,
    IOptions<MinifluxSettings> minifluxSettings,
    HealthCheckResultCache<MinifluxHealthCheck> cache) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!feedImportSettings.Value.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Feed import is disabled (FeedImport:Enabled): Miniflux is not checked."));
        }

        return cache.GetOrAddAsync(CheckMinifluxAsync, cancellationToken);
    }

    private async Task<HealthCheckResult> CheckMinifluxAsync(CancellationToken cancellationToken)
    {
        var categories = await minifluxClient.GetCategoriesAsync(cancellationToken);
        if (categories.IsFailure)
        {
            return HealthCheckResult.Degraded($"Miniflux check failed: {categories.Error!.Description}");
        }

        // Same matching as SyncFeedImportsCommandHandler.
        var categoryName = minifluxSettings.Value.CategoryName?.Trim() ?? string.Empty;
        var found = categories.Value!.Any(c => string.Equals(c.Title.Trim(), categoryName, StringComparison.OrdinalIgnoreCase));
        return found
            ? HealthCheckResult.Healthy($"Miniflux is reachable and category '{categoryName}' exists.")
            : HealthCheckResult.Degraded($"Miniflux is reachable but category '{categoryName}' was not found (Miniflux:CategoryName).");
    }
}

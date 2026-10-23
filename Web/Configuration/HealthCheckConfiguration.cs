using HealthChecks.ApplicationStatus.DependencyInjection;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Web.Infrastructure;

namespace Web.Configuration;

public static class HealthCheckConfiguration
{
    // Checks that only touch local resources: the Docker HEALTHCHECK probes /health/live every
    // 30 s, so third-party APIs (Cloudinary, Debrid-Link, Miniflux) must stay out of it.
    public const string LiveTag = "live";

    public static IServiceCollection AddAppHealthChecks(this IServiceCollection services, string connectionString)
    {
        // One singleton cache per check, shared across calls, so third-party APIs (Cloudinary,
        // Debrid-Link, Miniflux) aren't re-pinged on every hit; the checks themselves stay at
        // AddCheck<T>'s default lifetime since they depend on scoped/transient services.
        services.AddSingleton(typeof(HealthCheckResultCache<>));
        services
            .AddHealthChecks()
            .AddApplicationStatus(tags: [LiveTag])
            .AddNpgSql(connectionString, tags: [LiveTag])
            .AddCheck<ImportDirectoryHealthCheck>("import-directory", tags: [LiveTag])
            .AddCheck<CloudinaryHealthCheck>("cloudinary")
            .AddCheck<DebridLinkHealthCheck>("debrid-link")
            .AddCheck<MinifluxHealthCheck>("miniflux");
        return services;
    }

    // /health runs every check (external APIs included); /health/live only the local ones.
    public static IEndpointRouteBuilder MapAppHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = IsLiveCheck,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        return endpoints;
    }

    internal static bool IsLiveCheck(HealthCheckRegistration registration) => registration.Tags.Contains(LiveTag);
}

using Application.FeedImports;
using Hangfire;
using Microsoft.Extensions.Options;
using Web.Infrastructure;
using Web.Services;

namespace Web.Configuration;

public static class FeedImportConfiguration
{
    public static IServiceCollection AddFeedImport(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FeedImportSettings>()
            .Bind(configuration.GetSection("FeedImport"))
            .Validate(cfg => !cfg.Enabled || !string.IsNullOrWhiteSpace(cfg.UserEmail), "FeedImport:UserEmail is required when FeedImport:Enabled is true")
            .Validate(cfg => FeedImportSyncJob.IsValidInterval(cfg.SyncIntervalMinutes), "FeedImport:SyncIntervalMinutes must be between 1 and 59, or a multiple of 60 up to 1440")
            .ValidateOnStart();

        services.AddOptions<MinifluxSettings>()
            .Bind(configuration.GetSection("Miniflux"))
            .Validate<IOptions<FeedImportSettings>>(
                (cfg, feedImport) => !feedImport.Value.Enabled || IsHttpUri(cfg.BaseUrl),
                "Miniflux:BaseUrl must be an absolute http(s) URL when FeedImport:Enabled is true")
            .Validate<IOptions<FeedImportSettings>>(
                (cfg, feedImport) => !feedImport.Value.Enabled || !string.IsNullOrWhiteSpace(cfg.ApiKey),
                "Miniflux:ApiKey is required when FeedImport:Enabled is true (env: Miniflux__ApiKey)")
            .Validate<IOptions<FeedImportSettings>>(
                (cfg, feedImport) => !feedImport.Value.Enabled || !string.IsNullOrWhiteSpace(cfg.CategoryName),
                "Miniflux:CategoryName is required when FeedImport:Enabled is true")
            .ValidateOnStart();

        // Miniflux is reached over the internal Docker network (http://miniflux:8080): plain HTTP is accepted,
        // but only towards the single configured host.
        services.AddHttpClient<IMinifluxClient, MinifluxClient>((sp, client) =>
        {
            var miniflux = sp.GetRequiredService<IOptions<MinifluxSettings>>().Value;
            if (IsHttpUri(miniflux.BaseUrl))
            {
                client.BaseAddress = WithTrailingSlash(miniflux.BaseUrl!);
            }
            if (!string.IsNullOrWhiteSpace(miniflux.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-Auth-Token", miniflux.ApiKey);
            }
            client.Timeout = TimeSpan.FromSeconds(30);
        })
            .AddHttpMessageHandler(sp =>
            {
                var baseUrl = sp.GetRequiredService<IOptions<MinifluxSettings>>().Value.BaseUrl;
                var allowedHosts = IsHttpUri(baseUrl)
                    ? new HashSet<string>([baseUrl!.Host], StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                return new SsrfGuardHandler(allowedHosts, allowHttp: baseUrl?.Scheme == Uri.UriSchemeHttp);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });

        // Article pages: only the feed sites listed in FeedImport:AllowedSourceHosts (subdomains included), HTTPS only.
        services.AddHttpClient<IArticlePageFetcher, ArticlePageFetcher>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
            .AddHttpMessageHandler(sp =>
            {
                var sourceHosts = sp.GetRequiredService<IOptions<FeedImportSettings>>().Value.AllowedSourceHosts;
                return new SsrfGuardHandler(new HashSet<string>(sourceHosts, StringComparer.OrdinalIgnoreCase), allowSubdomains: true);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });

        services.AddScoped<FeedImportSyncJob>();
        services.AddScoped<IFeedImportService, FeedImportService>();

        return services;
    }

    public static void ScheduleFeedImportSync(this IServiceProvider services)
    {
        var settings = services.GetRequiredService<IOptions<FeedImportSettings>>().Value;
        FeedImportSyncJob.Schedule(services.GetRequiredService<IRecurringJobManager>(), settings);
    }

    private static bool IsHttpUri(Uri? uri) =>
        uri is { IsAbsoluteUri: true } && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static Uri WithTrailingSlash(Uri uri)
    {
        var builder = new UriBuilder(uri);
        if (!builder.Path.EndsWith('/'))
        {
            builder.Path += '/';
        }
        return builder.Uri;
    }
}

using Application.Abstractions.Messaging;
using Application.ComicInfoSearch;
using Application.FeedImports.Analysis;
using Application.FeedImports.Analysis.Sites;
using Application.FeedImports.Download;
using Application.Interfaces;
using Application.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationDependencyInjection).Assembly;

        services.Scan(scan => scan.FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false).AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false).AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false).AsImplementedInterfaces().WithScopedLifetime()
        );

        services.AddScoped<IComicSearchService, ComicSearchService>();
        services.AddScoped<FeedImportDownloadServices>();
        services.AddScoped<FeedImportDepositServices>();
        services.AddScoped<FeedImportDownloadOptions>();

        // Site-specific extractors are registered before the generic fallback.
        services.AddSingleton<IDownloadLinkExtractor, PlaneteBdLinkExtractor>();
        services.AddSingleton<IDownloadLinkExtractor, ZoneEbookLinkExtractor>();
        services.AddSingleton<IDownloadLinkExtractor, GenericDownloadLinkExtractor>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<FeatureToggleDefaults>();
        services.AddSingleton<FeatureToggles>();
        services.AddSingleton<IFeatureToggles>(sp => sp.GetRequiredService<FeatureToggles>());

        return services;
    }
}

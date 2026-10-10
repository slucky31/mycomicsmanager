using Application.Books.IsbnScan;
using Application.ComicInfoSearch;
using Application.FeedImports;
using Domain.Settings;
using Microsoft.Extensions.Options;

namespace Application.Settings;

// What the configuration (appsettings / environment variables) says about each feature.
// To add a feature: a FeatureToggle value, its line in IsConfiguredOn and, if it needs
// some configuration to run, its line in GetMissingConfiguration.
public sealed class FeatureToggleDefaults(
    IOptions<FeedImportSettings> feedImport,
    IOptions<MinifluxSettings> miniflux,
    IOptions<IsbnOcrSettings> isbnOcr,
    IOptions<BedethequeSettings> bedetheque)
{
    public bool IsConfiguredOn(FeatureToggle toggle) => toggle switch
    {
        FeatureToggle.FeedImport => feedImport.Value.Enabled,
        FeatureToggle.IsbnOcr => isbnOcr.Value.Enabled,
        FeatureToggle.Bedetheque => bedetheque.Value.Enabled,
        _ => false
    };

    // The configuration of a feature is only validated at startup when it is on: check it before turning it on.
    public string? GetMissingConfiguration(FeatureToggle toggle) => toggle switch
    {
        FeatureToggle.FeedImport => GetFeedImportMissingConfiguration(),
        FeatureToggle.IsbnOcr => string.IsNullOrWhiteSpace(isbnOcr.Value.TesseractPath) ? "IsbnOcr:TesseractPath is required." : null,
        _ => null
    };

    private string? GetFeedImportMissingConfiguration()
    {
        if (string.IsNullOrWhiteSpace(feedImport.Value.UserEmail))
        {
            return "FeedImport:UserEmail is required.";
        }

        var minifluxSettings = miniflux.Value;
        if (minifluxSettings.BaseUrl is not { IsAbsoluteUri: true } baseUrl
            || (baseUrl.Scheme != Uri.UriSchemeHttp && baseUrl.Scheme != Uri.UriSchemeHttps))
        {
            return "Miniflux:BaseUrl must be an absolute http(s) URL.";
        }

        if (string.IsNullOrWhiteSpace(minifluxSettings.ApiKey))
        {
            return "Miniflux:ApiKey is required.";
        }

        return string.IsNullOrWhiteSpace(minifluxSettings.CategoryName) ? "Miniflux:CategoryName is required." : null;
    }
}

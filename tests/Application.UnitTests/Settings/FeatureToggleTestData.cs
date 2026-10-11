using Application.Books.IsbnScan;
using Application.ComicInfoSearch;
using Application.FeedImports;
using Application.Settings;
using Microsoft.Extensions.Options;

namespace Application.UnitTests.Settings;

internal sealed class FeatureToggleTestData
{
    public FeedImportSettings FeedImport { get; } = new() { Enabled = false, UserEmail = "user@example.com" };

    public MinifluxSettings Miniflux { get; } = new() { BaseUrl = new Uri("http://miniflux:8080"), ApiKey = "key", CategoryName = "BD" };

    public IsbnOcrSettings IsbnOcr { get; } = new() { Enabled = true, TesseractPath = "tesseract" };

    public BedethequeSettings Bedetheque { get; set; } = CreateBedetheque(enabled: true);

    public static BedethequeSettings CreateBedetheque(bool enabled) => new()
    {
        BaseUrl = new Uri("https://www.bedetheque.com"),
        SerpApiBaseUrl = new Uri("https://serpapi.com"),
        Enabled = enabled
    };

    public FeatureToggleDefaults CreateDefaults() => new(
        Options.Create(FeedImport), Options.Create(Miniflux), Options.Create(IsbnOcr), Options.Create(Bedetheque));
}

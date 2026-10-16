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

    public BnfCatalogueSettings Bnf { get; set; } = new() { BaseUrl = new Uri("https://catalogue.bnf.fr") };

    public GoogleBooksSettings GoogleBooks { get; set; } = new() { BaseUrl = new Uri("https://www.googleapis.com/books/v1") };

    public OpenLibrarySettings OpenLibrary { get; set; } = new() { BaseUrl = new Uri("https://openlibrary.org"), CoversBaseUrl = new Uri("https://covers.openlibrary.org") };

    public BubbleSettings Bubble { get; set; } = CreateBubble(apiKey: "search-key");

    public static BubbleSettings CreateBubble(string apiKey, bool enabled = true) => new()
    {
        BaseUrl = new Uri("https://www.bubblebd.com"),
        AlgoliaBaseUrl = new Uri("https://6a891z72zd-dsn.algolia.net"),
        ApplicationId = "6A891Z72ZD",
        ApiKey = apiKey,
        Enabled = enabled
    };

    public static BedethequeSettings CreateBedetheque(bool enabled) => new()
    {
        BaseUrl = new Uri("https://www.bedetheque.com"),
        SerpApiBaseUrl = new Uri("https://serpapi.com"),
        Enabled = enabled
    };

    public FeatureToggleDefaults CreateDefaults() => new(
        Options.Create(FeedImport), Options.Create(Miniflux), Options.Create(IsbnOcr),
        new BookSourceOptions(Options.Create(Bnf), Options.Create(GoogleBooks), Options.Create(OpenLibrary), Options.Create(Bubble), Options.Create(Bedetheque)));
}

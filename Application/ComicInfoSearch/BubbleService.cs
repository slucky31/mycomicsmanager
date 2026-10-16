using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Application.Helpers;
using Application.Interfaces;
using Domain.Settings;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.ComicInfoSearch;

/// <summary>
/// Searches Bubble (bubblebd.com) by ISBN through the Algolia index of its site, which gives the series,
/// the volume, the title and the cover; then reads the album page: its schema.org Book (publisher, pages,
/// cover) and its "Caractéristiques" (authors, release date). A page that cannot be read keeps the search result.
/// </summary>
public partial class BubbleService : IBubbleService
{
    private const string AlbumsIndex = "Albums";
    private const int MaxHits = 5;

    private static readonly Dictionary<string, int> s_frenchMonths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["janvier"] = 1,
        ["février"] = 2,
        ["mars"] = 3,
        ["avril"] = 4,
        ["mai"] = 5,
        ["juin"] = 6,
        ["juillet"] = 7,
        ["août"] = 8,
        ["septembre"] = 9,
        ["octobre"] = 10,
        ["novembre"] = 11,
        ["décembre"] = 12,
    };

    private readonly HttpClient _httpClient;
    private readonly BubbleSettings _settings;
    private readonly IFeatureToggles _featureToggles;
    private readonly ILogger<BubbleService> _logger;

    public BubbleService(HttpClient httpClient, IOptions<BubbleSettings> settings, IFeatureToggles featureToggles, ILogger<BubbleService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _featureToggles = featureToggles;
        _logger = logger;
    }

    // "07 mai 2021", "1er juin 2019", "mai 2021" or "2021".
    [GeneratedRegex(@"^(?:(?<day>\d{1,2})(?:er)?\s+)?(?:(?<month>\p{L}+)\s+)?(?<year>\d{4})$")]
    private static partial Regex ReleaseDatePattern();

    // Permalinks and object ids of the search, put in the album URL.
    [GeneratedRegex("^[A-Za-z0-9-]+$")]
    private static partial Regex UrlSegmentPattern();

    public bool IsEnabled => _featureToggles.IsEnabled(FeatureToggle.Bubble) && _settings.HasApiKey;

    public async Task<BubbleBookResult> SearchByIsbnAsync(string isbn, CancellationToken ct = default)
    {
        var cleanIsbn = IsbnHelper.NormalizeIsbn(isbn);
        if (!IsEnabled || string.IsNullOrEmpty(cleanIsbn))
        {
            return CreateNotFoundResult();
        }

        try
        {
            _logger.LogInformation("Searching Bubble for ISBN: {Isbn}", cleanIsbn);

            using var response = await SearchAlbumsAsync(cleanIsbn, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Bubble search returned {StatusCode} for ISBN: {Isbn}", response.StatusCode, cleanIsbn);
                return CreateFailedResult();
            }

            var search = await response.Content.ReadFromJsonAsync<AlgoliaResponse>(JsonSerializerOptions.Default, ct);
            var hit = search?.Hits?.FirstOrDefault(h => HasEan(h, cleanIsbn));
            if (hit is null)
            {
                _logger.LogInformation("No album found on Bubble for ISBN: {Isbn}", cleanIsbn);
                return CreateNotFoundResult();
            }

            _logger.LogInformation("Found on Bubble for ISBN {Isbn}: {Serie} T{Volume} - {Title}", cleanIsbn, hit.SerieTitle, hit.Tome, hit.Title);
            var result = ToResult(hit);
            var page = await FetchAlbumPageAsync(hit, ct);
            return page is null ? result : AddPageDetails(result, page);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error searching Bubble for ISBN: {Isbn}", cleanIsbn);
            return CreateFailedResult();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error searching Bubble for ISBN: {Isbn}", cleanIsbn);
            return CreateFailedResult();
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout searching Bubble for ISBN: {Isbn}", cleanIsbn);
            return CreateFailedResult();
        }
    }

    private async Task<HttpResponseMessage> SearchAlbumsAsync(string isbn, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_settings.AlgoliaBaseUrl, $"/1/indexes/{AlbumsIndex}/query"))
        {
            Content = JsonContent.Create(new AlgoliaQuery(isbn, MaxHits)),
        };
        request.Headers.Add("X-Algolia-Application-Id", _settings.ApplicationId);
        request.Headers.Add("X-Algolia-API-Key", _settings.ApiKey);

        return await _httpClient.SendAsync(request, ct);
    }

    // The search is full text: only an album whose EANs hold the ISBN is the book.
    private static bool HasEan(AlgoliaHit hit, string isbn) =>
        hit.Eans?.Split([' ', ',', ';', '|'], StringSplitOptions.RemoveEmptyEntries)
            .Any(ean => string.Equals(IsbnHelper.NormalizeIsbn(ean), isbn, StringComparison.Ordinal)) == true;

    private static BubbleBookResult ToResult(AlgoliaHit hit) =>
        new(
            Title: hit.Title ?? hit.SerieTitle ?? string.Empty,
            Serie: hit.SerieTitle ?? string.Empty,
            VolumeNumber: hit.Tome ?? 1,
            Authors: [],
            Publishers: [],
            PublishDate: null,
            NumberOfPages: null,
            CoverUrl: ToUri(hit.ImageUrl),
            Found: true);

    private async Task<HtmlDocument?> FetchAlbumPageAsync(AlgoliaHit hit, CancellationToken ct)
    {
        var url = AlbumUrl(hit);
        if (url is null)
        {
            return null;
        }

        try
        {
            using var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Bubble album page returned {StatusCode}: {Url}", response.StatusCode, url);
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(await response.Content.ReadAsStringAsync(ct));
            return doc;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error fetching the Bubble album page: {Url}", url);
            return null;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Timeout fetching the Bubble album page: {Url}", url);
            return null;
        }
    }

    // https://www.bubblebd.com/{permalink}/album/{albumObjectId}/{printObjectId}
    private Uri? AlbumUrl(AlgoliaHit hit)
    {
        if (!IsUrlSegment(hit.Permalink) || !IsUrlSegment(hit.ObjectId))
        {
            return null;
        }

        var path = $"/{hit.Permalink}/album/{hit.ObjectId}";
        return new Uri(_settings.BaseUrl, IsUrlSegment(hit.DefaultSellingPrintObjectId) ? $"{path}/{hit.DefaultSellingPrintObjectId}" : path);
    }

    private static bool IsUrlSegment([NotNullWhen(true)] string? value) =>
        value is not null && UrlSegmentPattern().IsMatch(value);

    private BubbleBookResult AddPageDetails(BubbleBookResult result, HtmlDocument page)
    {
        var book = ReadSchemaBook(page);
        var publisher = book?.Publisher?.Name ?? ReadCharacteristic(page, "Editeur");
        return result with
        {
            Authors = ReadAuthors(page),
            Publishers = string.IsNullOrWhiteSpace(publisher) ? result.Publishers : [publisher.Trim()],
            PublishDate = ParseReleaseDate(ReadCharacteristic(page, "Date de parution")),
            NumberOfPages = book?.NumberOfPages,
            CoverUrl = ToUri(book?.Image) ?? result.CoverUrl,
        };
    }

    private SchemaBook? ReadSchemaBook(HtmlDocument page)
    {
        foreach (var script in page.DocumentNode.SelectNodes("//script[@type='application/ld+json']") ?? Enumerable.Empty<HtmlNode>())
        {
            try
            {
                using var json = JsonDocument.Parse(script.InnerText);
                var book = FindSchemaBook(json.RootElement);
                if (book is not null)
                {
                    return book.Value.Deserialize<SchemaBook>();
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Unreadable schema.org data in a Bubble album page");
            }
        }

        return null;
    }

    // The page describes the site, the page, the book and the product, in a "@graph".
    private static JsonElement? FindSchemaBook(JsonElement root)
    {
        IEnumerable<JsonElement> items = root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("@graph", out var graph) && graph.ValueKind == JsonValueKind.Array
                ? graph.EnumerateArray()
                : [root];

        foreach (var item in items)
        {
            if (item.ValueKind == JsonValueKind.Object
                && item.TryGetProperty("@type", out var type)
                && string.Equals(type.ToString(), "Book", StringComparison.Ordinal))
            {
                return item;
            }
        }

        return null;
    }

    // <div>Auteurs</div><div><a href="/joelle-jones/author/...">Joëlle Jones</a>, <a ...>Fernando Blanco</a></div>
    private static List<string> ReadAuthors(HtmlDocument page) =>
        page.DocumentNode.SelectNodes("//div[normalize-space(.)='Auteurs']/following-sibling::div[1]//a")
            ?.Select(a => HtmlEntity.DeEntitize(a.InnerText).Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];

    // <div>Date de parution</div><div>07 mai 2021</div>
    private static string? ReadCharacteristic(HtmlDocument page, string label)
    {
        var value = page.DocumentNode.SelectSingleNode($"//div[normalize-space(.)='{label}']/following-sibling::div[1]");
        return value is null ? null : HtmlEntity.DeEntitize(value.InnerText).Trim();
    }

    // Convention of the application: a month alone is its first day, a year alone January 1st.
    internal static DateOnly? ParseReleaseDate(string? value)
    {
        var match = value is null ? Match.Empty : ReleaseDatePattern().Match(value.Trim());
        if (!match.Success)
        {
            return null;
        }

        var month = 1;
        if (match.Groups["month"].Success && !s_frenchMonths.TryGetValue(match.Groups["month"].Value, out month))
        {
            return null;
        }

        var year = int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture);
        var day = match.Groups["day"].Success ? int.Parse(match.Groups["day"].Value, CultureInfo.InvariantCulture) : 1;
        return day >= 1 && day <= DateTime.DaysInMonth(year, month) ? new DateOnly(year, month, day) : null;
    }

    private static Uri? ToUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? uri : null;

    private static BubbleBookResult CreateFailedResult() => CreateNotFoundResult() with { Failed = true };

    private static BubbleBookResult CreateNotFoundResult() =>
        new(string.Empty, string.Empty, 0, [], [], null, null, null, Found: false);

    // Internal DTOs for JSON (de)serialization
    private sealed record AlgoliaQuery(
        [property: JsonPropertyName("query")] string Query,
        [property: JsonPropertyName("hitsPerPage")] int HitsPerPage
    );

    private sealed record AlgoliaResponse(
        [property: JsonPropertyName("hits")] IReadOnlyList<AlgoliaHit>? Hits
    );

    private sealed record AlgoliaHit(
        [property: JsonPropertyName("objectId")] string? ObjectId,
        [property: JsonPropertyName("permalink")] string? Permalink,
        [property: JsonPropertyName("defaultSellingPrintObjectId")] string? DefaultSellingPrintObjectId,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("tome")] int? Tome,
        [property: JsonPropertyName("serieTitle")] string? SerieTitle,
        [property: JsonPropertyName("eans")] string? Eans,
        [property: JsonPropertyName("imageUrl")] string? ImageUrl
    );

    private sealed record SchemaBook(
        [property: JsonPropertyName("numberOfPages")] int? NumberOfPages,
        [property: JsonPropertyName("publisher")] SchemaOrganization? Publisher,
        [property: JsonPropertyName("image")] string? Image
    );

    private sealed record SchemaOrganization(
        [property: JsonPropertyName("name")] string? Name
    );
}

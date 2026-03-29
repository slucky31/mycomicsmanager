using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Application.Helpers;
using Application.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;

namespace Application.ComicInfoSearch;

public partial class BedethequeService : IBedethequeService
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<BedethequeService>();

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIsbnBedethequeCacheRepository _cacheRepository;
    private readonly BedethequeSettings _settings;
    private readonly string _bdUrlPrefix;
    private readonly string _coversBaseUrl;
    private readonly string _serieUrlPrefix;
    private readonly string _serieBdUrlPrefix;

    public BedethequeService(
        IHttpClientFactory httpClientFactory,
        IIsbnBedethequeCacheRepository cacheRepository,
        IOptions<BedethequeSettings> settings)
    {
        _httpClientFactory = httpClientFactory;
        _cacheRepository = cacheRepository;
        _settings = settings.Value;
        var baseUrl = _settings.BaseUrl.ToString().TrimEnd('/');
        _bdUrlPrefix = $"{baseUrl}/BD";
        _coversBaseUrl = $"{baseUrl}/media/Couvertures/Couv_";
        _serieUrlPrefix = $"{baseUrl}/serie-";
        _serieBdUrlPrefix = $"{baseUrl}/serie-bd";
    }

    public async Task<BedethequeBookResult> SearchByIsbnAsync(string isbn, CancellationToken ct = default)
    {
        var cleanIsbn = IsbnHelper.NormalizeIsbn(isbn);

        try
        {
            var cachedUrl = await _cacheRepository.GetUrlByIsbnAsync(cleanIsbn, ct);
            if (cachedUrl is not null)
            {
                Log.Information("Cache hit for ISBN {Isbn}: {Url}", cleanIsbn, cachedUrl);
            }

            var url = cachedUrl ?? await ResolveUrlAsync(cleanIsbn, ct);
            if (url is null)
            {
                return CreateNotFoundResult();
            }

            Log.Information("Fetching Bedetheque page: {Url}", url);
            var html = await FetchPageAsync(url, ct);
            if (html is null)
            {
                return CreateNotFoundResult();
            }

            var result = ParsePage(html, url, _coversBaseUrl);

            if (result.Found && cachedUrl is null)
            {
                await _cacheRepository.SaveAsync(cleanIsbn, url, ct);
                Log.Information("Cached Bedetheque URL for ISBN {Isbn}: {Url}", cleanIsbn, url);
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error searching Bedetheque for ISBN: {Isbn}", cleanIsbn);
            return CreateNotFoundResult();
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "JSON parsing error for ISBN: {Isbn}", cleanIsbn);
            return CreateNotFoundResult();
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            Log.Error(ex, "Timeout searching Bedetheque for ISBN: {Isbn}", cleanIsbn);
            return CreateNotFoundResult();
        }
    }

    private async Task<string?> ResolveUrlAsync(string isbn, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.SerpApiKey))
        {
            Log.Warning("SerpApi key is not configured, skipping Bedetheque search for ISBN {Isbn}", isbn);
            return null;
        }

        string?[] candidates = [isbn, IsbnHelper.ToHyphenatedIsbn(isbn), IsbnHelper.ToShortIsbn(isbn)];
        foreach (var searchIsbn in candidates)
        {
            if (string.IsNullOrEmpty(searchIsbn))
            {
                continue;
            }
            var result = await TrySearchFormatAsync(searchIsbn, isbn, ct);
            if (result is not null)
            {
                return result;
            }
        }
        return null;
    }

    private async Task<string?> TrySearchFormatAsync(string searchIsbn, string normalizedIsbn, CancellationToken ct)
    {
        var serpUrl = BuildSerpApiUrl(searchIsbn);
        Log.Information("Calling SerpApi for ISBN {Isbn} (search: {SearchIsbn})", normalizedIsbn, searchIsbn);

        var serpClient = _httpClientFactory.CreateClient("SerpApi");
        var response = await serpClient.GetAsync(new Uri(serpUrl), ct);
        if (!response.IsSuccessStatusCode)
        {
            Log.Warning("SerpApi returned {StatusCode} for ISBN {SearchIsbn}", response.StatusCode, searchIsbn);
            return null;
        }

        var serpResult = await response.Content.ReadFromJsonAsync<SerpApiResponse>(JsonOptions, ct);
        var links = serpResult?.OrganicResults?.Select(r => r.Link).OfType<string>().ToList() ?? [];

        var bdLinks = links
            .Where(l => l.StartsWith(_bdUrlPrefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (bdLinks.Count == 1)
        {
            return bdLinks[0];
        }

        var serieLinks = links
            .Where(l => l.StartsWith(_serieUrlPrefix, StringComparison.OrdinalIgnoreCase) &&
                        !l.StartsWith(_serieBdUrlPrefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (serieLinks.Count == 1)
        {
            var albumUrl = await ResolveSeriePageAsync(serieLinks[0], normalizedIsbn, ct);
            if (albumUrl is not null)
            {
                return albumUrl;
            }
        }

        Log.Warning("No match for search ISBN {SearchIsbn} ({BdCount} BD, {SerieCount} serie links)",
            searchIsbn, bdLinks.Count, serieLinks.Count);
        return null;
    }

    private async Task<string?> ResolveSeriePageAsync(string serieUrl, string isbn, CancellationToken ct)
    {
        Log.Information("Parsing serie page for ISBN {Isbn}: {SerieUrl}", isbn, serieUrl);
        var html = await FetchPageAsync(serieUrl, ct);
        if (html is null)
        {
            return null;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var items = doc.DocumentNode.SelectNodes(
            "//li[@itemscope and @itemtype='https://schema.org/Book']");
        if (items is null)
        {
            return null;
        }

        foreach (var item in items)
        {
            var isbnSpan = item.SelectSingleNode(".//span[@itemprop='isbn']");
            if (isbnSpan is null)
            {
                continue;
            }
            var pageIsbn = IsbnHelper.NormalizeIsbn(HtmlEntity.DeEntitize(isbnSpan.InnerText.Trim()));
            if (!pageIsbn.Equals(isbn, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var anchor = item.SelectSingleNode(".//a[@itemprop='url' and contains(@class,'titre')]");
            var href = anchor?.GetAttributeValue("href", string.Empty);
            if (!string.IsNullOrEmpty(href))
            {
                Log.Information("Found album URL via serie page for ISBN {Isbn}: {Href}", isbn, href);
                return href;
            }
        }

        Log.Warning("ISBN {Isbn} not found in serie page {SerieUrl}", isbn, serieUrl);
        return null;
    }

    private string BuildSerpApiUrl(string isbn)
    {
        var query = Uri.EscapeDataString($"{isbn} site:bedetheque.com");
        return $"{_settings.SerpApiBaseUrl}/search.json?engine=google&q={query}&location=France&google_domain=google.fr&hl=fr&gl=fr&api_key={_settings.SerpApiKey}";
    }

    private async Task<string?> FetchPageAsync(string url, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("Bedetheque");
        var response = await client.GetAsync(new Uri(url), ct);

        if (!response.IsSuccessStatusCode)
        {
            Log.Warning("Bedetheque page returned {StatusCode} for URL: {Url}", response.StatusCode, url);
            return null;
        }

        return await response.Content.ReadAsStringAsync(ct);
    }

    private static BedethequeBookResult ParsePage(string html, string pageUrl, string coversBaseUrl)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var title = ParseTitle(doc);
        var serie = ParseSerie(doc);
        var volumeNumber = ParseVolumeNumber(doc);
        var authors = ParseAuthors(doc);
        var publishers = ParsePublishers(doc);
        var publishDate = ParsePublishDate(doc);
        var numberOfPages = ParseNumberOfPages(doc);
        var coverUrl = BuildCoverUrl(pageUrl, coversBaseUrl);

        Log.Information("Parsed Bedetheque page: {Serie} T{Volume} - {Title}", serie, volumeNumber, title);

        return new BedethequeBookResult(
            Title: title,
            Serie: serie,
            VolumeNumber: volumeNumber,
            Authors: authors,
            Publishers: publishers,
            PublishDate: publishDate,
            NumberOfPages: numberOfPages,
            CoverUrl: coverUrl,
            Found: !string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(serie)
        );
    }

    // Returns the first non-empty text node inside a <li> from infos-albums whose <label> contains the given text.
    private static string? GetInfoAlbumFieldText(HtmlDocument doc, string labelContains)
    {
        var li = doc.DocumentNode.SelectSingleNode(
            $"//ul[contains(@class,'infos-albums')]//li[label[contains(.,'{labelContains}')]]");

        return li?.SelectNodes("text()")
            ?.Select(n => HtmlEntity.DeEntitize(n.InnerText.Trim()))
            .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
    }

    private static string ParseTitle(HtmlDocument doc) =>
        GetInfoAlbumFieldText(doc, "Titre") ?? string.Empty;

    private static string ParseSerie(HtmlDocument doc) =>
        GetInfoAlbumFieldText(doc, "Série") ?? string.Empty;

    private static int ParseVolumeNumber(HtmlDocument doc)
    {
        var text = GetInfoAlbumFieldText(doc, "Tome");
        return text is not null && int.TryParse(text, out var vol) ? vol : 1;
    }

    private static List<string> ParseAuthors(HtmlDocument doc)
    {
        var authors = new List<string>();

        var scenarioLi = doc.DocumentNode.SelectSingleNode(
            "//ul[contains(@class,'infos-albums')]//li[label[contains(.,'Scénario')]]");
        var scenarioAnchors = scenarioLi?.SelectNodes(".//a");
        if (scenarioAnchors != null)
        {
            foreach (var anchor in scenarioAnchors)
            {
                var name = HtmlEntity.DeEntitize(anchor.InnerText.Trim());
                if (!string.IsNullOrWhiteSpace(name) && !authors.Contains(name))
                {
                    authors.Add(name);
                }
            }
        }

        var dessinLi = doc.DocumentNode.SelectSingleNode(
            "//ul[contains(@class,'infos-albums')]//li[label[contains(.,'Dessin')]]");
        var dessinAnchors = dessinLi?.SelectNodes(".//a");
        if (dessinAnchors != null)
        {
            foreach (var anchor in dessinAnchors)
            {
                var name = HtmlEntity.DeEntitize(anchor.InnerText.Trim());
                if (!string.IsNullOrWhiteSpace(name) && !authors.Contains(name))
                {
                    authors.Add(name);
                }
            }
        }

        return authors;
    }

    private static IReadOnlyList<string> ParsePublishers(HtmlDocument doc)
    {
        var li = doc.DocumentNode.SelectSingleNode(
            "//ul[contains(@class,'infos-albums')]//li[label[contains(.,'diteur')]]");
        if (li is null)
        {
            return [];
        }

        // Publisher is wrapped in a <span> or as a direct text node
        var span = li.SelectSingleNode(".//span");
        var text = span is not null
            ? HtmlEntity.DeEntitize(span.InnerText.Trim())
            : GetInfoAlbumFieldText(doc, "diteur");

        return string.IsNullOrWhiteSpace(text) ? [] : [text];
    }

    // Matches "Parution le DD/MM/YYYY" (inside the grise span on the Dépot légal line)
    [GeneratedRegex(@"Parution\s+le\s+(\d{2}/\d{2}/\d{4})", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ParutionDateRegex();

    // Matches "MM/YYYY" for the Dépot légal field text node
    [GeneratedRegex(@"(\d{2})/(\d{4})", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MonthYearRegex();

    private static DateOnly? ParsePublishDate(HtmlDocument doc)
    {
        // The Dépot/Dépôt légal <li> — "légal" is common to both spelling variants
        var li = doc.DocumentNode.SelectSingleNode(
            "//ul[contains(@class,'infos-albums')]//li[label[contains(.,'gal')]]");
        if (li is null)
        {
            return null;
        }

        // Strategy 1: "Parution le DD/MM/YYYY" from the <span class="grise">
        var griseSpan = li.SelectSingleNode(".//span[contains(@class,'grise')]");
        if (griseSpan is not null)
        {
            var parutionMatch = ParutionDateRegex().Match(griseSpan.InnerText);
            if (parutionMatch.Success)
            {
                var parsed = PublishDateHelper.ParsePublishDate(parutionMatch.Groups[1].Value);
                if (parsed is not null)
                {
                    return parsed;
                }
            }
        }

        // Strategy 2: "MM/YYYY" from the direct text node (e.g. "08/2014")
        var textNode = li.SelectNodes("text()")
            ?.Select(n => n.InnerText.Trim())
            .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));

        if (textNode is not null)
        {
            var mmYyyy = MonthYearRegex().Match(textNode);
            if (mmYyyy.Success &&
                int.TryParse(mmYyyy.Groups[1].Value, out var month) &&
                int.TryParse(mmYyyy.Groups[2].Value, out var year) &&
                month is >= 1 and <= 12 &&
                year is >= 1900 and <= 2100)
            {
                return new DateOnly(year, month, 1);
            }
        }

        return null;
    }

    private static int? ParseNumberOfPages(HtmlDocument doc)
    {
        var li = doc.DocumentNode.SelectSingleNode(
            "//ul[contains(@class,'infos-albums')]//li[label[contains(.,'Planches')]]");
        if (li is null)
        {
            return null;
        }

        var span = li.SelectSingleNode(".//span");
        var text = span?.InnerText.Trim() ?? GetInfoAlbumFieldText(doc, "Planches");
        return text is not null && int.TryParse(text, out var pages) ? pages : null;
    }

    [GeneratedRegex(@"-(\d+)\.html$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PageIdRegex();

    private static Uri? BuildCoverUrl(string pageUrl, string coversBaseUrl)
    {
        var match = PageIdRegex().Match(pageUrl);
        if (!match.Success)
        {
            return null;
        }

        return new Uri($"{coversBaseUrl}{match.Groups[1].Value}.jpg");
    }

    private static BedethequeBookResult CreateNotFoundResult() =>
        new(
            Title: string.Empty,
            Serie: string.Empty,
            VolumeNumber: 1,
            Authors: [],
            Publishers: [],
            PublishDate: null,
            NumberOfPages: null,
            CoverUrl: null,
            Found: false
        );

    private static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Internal DTOs for SerpApi JSON deserialization
    private sealed record SerpApiResponse(
        [property: JsonPropertyName("organic_results")] IReadOnlyList<SerpApiOrganicResult>? OrganicResults
    );

    private sealed record SerpApiOrganicResult(
        [property: JsonPropertyName("link")] string? Link
    );
}

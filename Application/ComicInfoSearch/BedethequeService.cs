using System.Net;
using System.Net.Http.Headers;
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

public partial class BedethequeService : IBedethequeService
{
    // Bedetheque shows every album of a serie on this page instead of 10 per page.
    private const string AllAlbumsSuffix = "__10000.html";

    // Browser Run free plan accepts one request every 10 seconds: a serie page then its album page wait once.
    private static readonly TimeSpan MaxBrowserRunRetryDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan DefaultBrowserRunRetryDelay = TimeSpan.FromSeconds(10);

    // The album data is text: the browser does not need to load what makes the page look nice.
    private static readonly string[] BrowserRunRejectedResources = ["image", "media", "font", "stylesheet"];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIsbnBedethequeCacheRepository _cacheRepository;
    private readonly BedethequeSettings _settings;
    private readonly string _bdUrlPrefix;
    private readonly string _coversBaseUrl;
    private readonly string _serieUrlPrefix;
    private readonly string _serieBdUrlPrefix;
    private readonly ILogger<BedethequeService> _logger;
    private readonly BedethequeCircuit _circuit;
    private readonly IFeatureToggles _featureToggles;

    public BedethequeService(
        IHttpClientFactory httpClientFactory,
        IIsbnBedethequeCacheRepository cacheRepository,
        IOptions<BedethequeSettings> settings,
        BedethequeCircuit circuit,
        IFeatureToggles featureToggles,
        ILogger<BedethequeService> logger)
    {
        _logger = logger;
        _circuit = circuit;
        _featureToggles = featureToggles;
        _httpClientFactory = httpClientFactory;
        _cacheRepository = cacheRepository;
        _settings = settings.Value;
        var baseUrl = _settings.BaseUrl.ToString().TrimEnd('/');
        _bdUrlPrefix = $"{baseUrl}/BD";
        _coversBaseUrl = $"{baseUrl}/media/Couvertures/Couv_";
        _serieUrlPrefix = $"{baseUrl}/serie-";
        _serieBdUrlPrefix = $"{baseUrl}/serie-bd";
    }

    public bool IsEnabled => _featureToggles.IsEnabled(FeatureToggle.Bedetheque);

    public async Task<BedethequeBookResult> SearchByIsbnAsync(string isbn, CancellationToken ct = default)
    {
        var cleanIsbn = IsbnHelper.NormalizeIsbn(isbn);
        if (!IsEnabled)
        {
            return CreateNotFoundResult();
        }

        if (_circuit.PausedUntil is { } pausedUntil)
        {
            _logger.LogInformation("Bedetheque is paused until {PausedUntil} after a Cloudflare challenge: ISBN {Isbn} not searched",
                pausedUntil, cleanIsbn);
            return CreateFailedResult();
        }

        try
        {
            var cachedUrl = await _cacheRepository.GetUrlByIsbnAsync(cleanIsbn, ct);
            if (cachedUrl is not null)
            {
                _logger.LogInformation("Cache hit for ISBN {Isbn}: {Url}", cleanIsbn, cachedUrl);
            }

            var url = cachedUrl ?? await ResolveUrlAsync(cleanIsbn, ct);
            if (url is null)
            {
                // A serie page blocked by Cloudflare pauses Bedetheque: the album may exist.
                return _circuit.PausedUntil is null ? CreateNotFoundResult() : CreateFailedResult();
            }

            _logger.LogInformation("Fetching Bedetheque page: {Url}", url);
            var html = await FetchPageAsync(url, ct);
            // The album page exists but could not be read (blocked or in error).
            if (html is null)
            {
                return CreateFailedResult();
            }

            var result = ParsePage(html, url, _coversBaseUrl);

            if (result.Found && cachedUrl is null)
            {
                await _cacheRepository.SaveAsync(cleanIsbn, url, ct);
                _logger.LogInformation("Cached Bedetheque URL for ISBN {Isbn}: {Url}", cleanIsbn, url);
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error searching Bedetheque for ISBN: {Isbn}", cleanIsbn);
            return CreateFailedResult();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for ISBN: {Isbn}", cleanIsbn);
            return CreateFailedResult();
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout searching Bedetheque for ISBN: {Isbn}", cleanIsbn);
            return CreateFailedResult();
        }
    }

    private async Task<string?> ResolveUrlAsync(string isbn, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.SerpApiKey))
        {
            _logger.LogWarning("SerpApi key is not configured, skipping Bedetheque search for ISBN {Isbn}", isbn);
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
        _logger.LogInformation("Calling SerpApi for ISBN {Isbn} (search: {SearchIsbn})", normalizedIsbn, searchIsbn);

        var serpClient = _httpClientFactory.CreateClient("SerpApi");
        var response = await serpClient.GetAsync(new Uri(serpUrl), ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("SerpApi returned {StatusCode} for ISBN {SearchIsbn}", response.StatusCode, searchIsbn);
            return null;
        }

        var serpResult = await response.Content.ReadFromJsonAsync<SerpApiResponse>(JsonOptions, ct);
        var links = serpResult?.OrganicResults?.Select(r => r.Link).OfType<string>()
                        .Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [];

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

        _logger.LogWarning("No match for search ISBN {SearchIsbn} ({BdCount} BD, {SerieCount} serie links)",
            searchIsbn, bdLinks.Count, serieLinks.Count);
        return null;
    }

    private async Task<string?> ResolveSeriePageAsync(string serieUrl, string isbn, CancellationToken ct)
    {
        // The first page of a serie only lists 10 albums: later volumes, HS and integrals are on the next ones.
        var allAlbumsUrl = SeriePageSuffixRegex().Replace(serieUrl, AllAlbumsSuffix);
        _logger.LogInformation("Parsing serie page for ISBN {Isbn}: {SerieUrl}", isbn, allAlbumsUrl);
        var html = await FetchPageAsync(allAlbumsUrl, ct);
        if (html is null)
        {
            return null;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Bedetheque has used both http:// and https:// schema.org types.
        var items = doc.DocumentNode.SelectNodes("//*[@itemscope and contains(@itemtype, 'schema.org/Book')]");
        if (items is null)
        {
            _logger.LogWarning("No album list found in serie page {SerieUrl} for ISBN {Isbn}", allAlbumsUrl, isbn);
            return null;
        }

        var href = FindAlbumUrl(items, isbn);
        if (href is null)
        {
            _logger.LogWarning("ISBN {Isbn} not found in serie page {SerieUrl}", isbn, allAlbumsUrl);
            return null;
        }

        _logger.LogInformation("Found album URL via serie page for ISBN {Isbn}: {Href}", isbn, href);
        return href;
    }

    // A deluxe printing (TL1) may share its ISBN with the regular volume (1): the regular volume wins.
    private static string? FindAlbumUrl(HtmlNodeCollection items, string isbn)
    {
        string? firstMatch = null;
        foreach (var item in items)
        {
            var isbnNode = item.SelectSingleNode(".//*[@itemprop='isbn']");
            if (isbnNode is null ||
                !IsbnHelper.NormalizeIsbn(HtmlEntity.DeEntitize(isbnNode.InnerText.Trim())).Equals(isbn, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var href = item.SelectSingleNode(".//a[@itemprop='url']")?.GetAttributeValue("href", string.Empty);
            if (string.IsNullOrEmpty(href))
            {
                continue;
            }

            if (IsRegularVolume(item))
            {
                return href;
            }
            firstMatch ??= href;
        }
        return firstMatch;
    }

    // The name of an album in a serie page starts with its volume: "1. Title", "HS. Title", "TL1. Title".
    private static bool IsRegularVolume(HtmlNode item)
    {
        var name = item.SelectSingleNode(".//*[@itemprop='name']");
        return name is not null && RegularVolumeRegex().IsMatch(HtmlEntity.DeEntitize(name.InnerText));
    }

    private string BuildSerpApiUrl(string isbn)
    {
        var query = Uri.EscapeDataString($"{isbn} site:bedetheque.com");
        return $"{_settings.SerpApiBaseUrl}/search.json?engine=google&q={query}&location=France&google_domain=google.fr&hl=fr&gl=fr&api_key={_settings.SerpApiKey}";
    }

    private Task<string?> FetchPageAsync(string url, CancellationToken ct) =>
        _settings.UseBrowserRun ? FetchThroughBrowserRunAsync(url, ct) : FetchDirectlyAsync(url, ct);

    private async Task<string?> FetchDirectlyAsync(string url, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("Bedetheque");
        var response = await client.GetAsync(new Uri(url), ct);

        var html = await response.Content.ReadAsStringAsync(ct);
        if (response.Headers.Contains("cf-mitigated") || IsCloudflareChallenge(html))
        {
            PauseAfterChallenge(url);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Bedetheque page returned {StatusCode} for URL: {Url}", response.StatusCode, url);
            return null;
        }

        return html;
    }

    private async Task<string?> FetchThroughBrowserRunAsync(string url, CancellationToken ct)
    {
        var response = await SendBrowserRunRequestAsync(url, ct);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var delay = GetRetryDelay(response);
            _logger.LogInformation("Browser Run rate limit reached, retrying {Url} in {Delay}", url, delay);
            await Task.Delay(delay, ct);
            response.Dispose();
            response = await SendBrowserRunRequestAsync(url, ct);
        }

        using var _ = response;
        var content = await response.Content.ReadFromJsonAsync<BrowserRunResponse>(JsonOptions, ct);
        if (!response.IsSuccessStatusCode || content is not { Success: true, Result: { } html })
        {
            var errors = string.Join("; ", content?.Errors?.Select(e => $"{e.Code} {e.Message}") ?? []);
            _logger.LogWarning("Browser Run returned {StatusCode} for {Url}: {Errors}", response.StatusCode, url, errors);
            return null;
        }

        // Bedetheque may one day challenge the browser too: Browser Run cannot pass it either.
        if (IsCloudflareChallenge(html))
        {
            PauseAfterChallenge(url);
            return null;
        }

        _logger.LogInformation("Fetched {Url} through Browser Run ({BrowserMs} ms of browser time)",
            url, response.Headers.TryGetValues("X-Browser-Ms-Used", out var used) ? used.FirstOrDefault() : "?");
        return html;
    }

    private async Task<HttpResponseMessage> SendBrowserRunRequestAsync(string url, CancellationToken ct)
    {
        var endpoint = new Uri(
            $"{_settings.BrowserRunBaseUrl.ToString().TrimEnd('/')}/accounts/{Uri.EscapeDataString(_settings.CloudflareAccountId)}/browser-run/content");
        // Waiting for the network to be idle never ends with the ads: the album data is in the HTML sent by the server.
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new
            {
                url,
                gotoOptions = new { waitUntil = "domcontentloaded", timeout = 45000 },
                rejectResourceTypes = BrowserRunRejectedResources
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.CloudflareApiToken);

        var client = _httpClientFactory.CreateClient("BrowserRun");
        return await client.SendAsync(request, ct);
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response)
    {
        var delay = response.Headers.RetryAfter?.Delta ?? DefaultBrowserRunRetryDelay;
        return delay > MaxBrowserRunRetryDelay ? MaxBrowserRunRetryDelay : delay;
    }

    private void PauseAfterChallenge(string url)
    {
        var until = _circuit.Pause(TimeSpan.FromHours(_settings.CloudflarePauseHours));
        _logger.LogWarning(
            "Bedetheque answered with a Cloudflare challenge instead of the page {Url}: the server's requests are blocked, Bedetheque is paused until {PausedUntil}",
            url, until);
    }

    // The "Just a moment..." page asks the browser to run a script: no album can be read from it.
    private static bool IsCloudflareChallenge(string html) =>
        html.Contains("Just a moment", StringComparison.OrdinalIgnoreCase)
        && html.Contains("cloudflare", StringComparison.OrdinalIgnoreCase);

    private BedethequeBookResult ParsePage(string html, string pageUrl, string coversBaseUrl)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var sheet = ReadAlbumSheet(doc);

        var title = GetSheetText(sheet, "Titre") ?? string.Empty;
        var serie = GetSheetText(sheet, "Série") ?? string.Empty;
        var volumeNumber = ParseVolumeNumber(sheet);
        var authors = ParseAuthors(sheet);
        var publisher = GetSheetText(sheet, "diteur");
        var publishDate = ParsePublishDate(sheet);
        var numberOfPages = int.TryParse(GetSheetText(sheet, "Planches"), out var pages) ? pages : (int?)null;
        var coverUrl = BuildCoverUrl(pageUrl, coversBaseUrl);

        _logger.LogInformation("Parsed Bedetheque page: {Serie} T{Volume} - {Title}", serie, volumeNumber, title);

        return new BedethequeBookResult(
            Title: title,
            Serie: serie,
            VolumeNumber: volumeNumber,
            Authors: authors,
            Publishers: string.IsNullOrWhiteSpace(publisher) ? [] : [publisher],
            PublishDate: publishDate,
            NumberOfPages: numberOfPages,
            CoverUrl: coverUrl,
            Found: !string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(serie)
        );
    }

    // The album sheet ("Informations sur l'album") lists its fields as <dt>label</dt><dd>value</dd>; a <dt> left
    // empty continues the previous field (a second colorist). The editions of the album have compact sheets.
    private static List<(string Label, HtmlNode Value)> ReadAlbumSheet(HtmlDocument doc)
    {
        var fields = new List<(string Label, HtmlNode Value)>();
        var sheet = doc.DocumentNode.SelectSingleNode(
            "//dl[contains(concat(' ', normalize-space(@class), ' '), ' bdt-sheet ') and not(contains(@class, 'bdt-sheet--compact'))]");
        if (sheet is null)
        {
            return fields;
        }

        var label = string.Empty;
        foreach (var node in sheet.ChildNodes.Where(n => n.Name is "dt" or "dd"))
        {
            if (node.Name == "dt")
            {
                var text = CleanText(node);
                label = text.Length > 0 ? text : label;
            }
            else
            {
                fields.Add((label, node));
            }
        }
        return fields;
    }

    private static IEnumerable<HtmlNode> GetSheetValues(List<(string Label, HtmlNode Value)> sheet, string labelContains) =>
        sheet.Where(f => f.Label.Contains(labelContains, StringComparison.OrdinalIgnoreCase)).Select(f => f.Value);

    private static string? GetSheetText(List<(string Label, HtmlNode Value)> sheet, string labelContains) =>
        GetSheetValues(sheet, labelContains).Select(CleanText).FirstOrDefault(t => t.Length > 0);

    private static string CleanText(HtmlNode node) =>
        WhitespaceRegex().Replace(HtmlEntity.DeEntitize(node.InnerText), " ").Trim();

    private static int ParseVolumeNumber(List<(string Label, HtmlNode Value)> sheet) =>
        int.TryParse(GetSheetText(sheet, "Tome"), out var volume) ? volume : 1;

    private static List<string> ParseAuthors(List<(string Label, HtmlNode Value)> sheet) =>
        GetSheetValues(sheet, "Scénario").Concat(GetSheetValues(sheet, "Dessin"))
            .SelectMany(dd => dd.SelectNodes(".//a") ?? Enumerable.Empty<HtmlNode>())
            .Select(CleanText)
            .Where(name => name.Length > 0)
            .Distinct()
            .ToList();

    // Matches "parution 03/01/2007" (new pages) and "Parution le 03/01/2007" (older ones)
    [GeneratedRegex(@"parution\s+(?:le\s+)?(\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ParutionDateRegex();

    // Matches "MM/YYYY" for the Dépôt légal field
    [GeneratedRegex(@"(\d{2})/(\d{4})", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MonthYearRegex();

    private DateOnly? ParsePublishDate(List<(string Label, HtmlNode Value)> sheet)
    {
        // The Dépôt/Dépot légal field — "légal" is common to both spelling variants
        var text = GetSheetText(sheet, "gal");
        if (text is null)
        {
            return null;
        }

        // Strategy 1: the release day, "01/2007 · parution 03/01/2007"
        var parutionMatch = ParutionDateRegex().Match(text);
        if (parutionMatch.Success &&
            PublishDateHelper.ParsePublishDate(parutionMatch.Groups[1].Value, _logger) is { } parution)
        {
            return parution;
        }

        // Strategy 2: the month of the legal deposit, "08/2014"
        var mmYyyy = MonthYearRegex().Match(text);
        if (mmYyyy.Success &&
            int.TryParse(mmYyyy.Groups[1].Value, out var month) &&
            int.TryParse(mmYyyy.Groups[2].Value, out var year) &&
            month is >= 1 and <= 12 &&
            year is >= 1900 and <= 2100)
        {
            return new DateOnly(year, month, 1);
        }

        return null;
    }

    [GeneratedRegex(@"-(\d+)\.html$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PageIdRegex();

    // "serie-10529-BD-Okko.html" or one of its pages, "serie-10529-BD-Okko__1.html"
    [GeneratedRegex(@"(__\d+)?\.html$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SeriePageSuffixRegex();

    [GeneratedRegex(@"^\s*\d+\s*\.", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex RegularVolumeRegex();

    [GeneratedRegex(@"\s+", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex WhitespaceRegex();

    private static Uri? BuildCoverUrl(string pageUrl, string coversBaseUrl)
    {
        var match = PageIdRegex().Match(pageUrl);
        if (!match.Success)
        {
            return null;
        }

        return new Uri($"{coversBaseUrl}{match.Groups[1].Value}.jpg");
    }

    private static BedethequeBookResult CreateFailedResult() => CreateNotFoundResult() with { Failed = true };

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

    // Browser Run /content answer: the HTML of the page once loaded, or the errors
    private sealed record BrowserRunResponse(bool Success, string? Result, IReadOnlyList<BrowserRunError>? Errors);

    private sealed record BrowserRunError(int Code, string? Message);
}

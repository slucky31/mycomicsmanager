using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Helpers;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.ComicInfoSearch;

public class GoogleBooksService : IGoogleBooksService
{
    private readonly HttpClient _httpClient;
    private const string SearchPath = "/volumes?q=isbn:";
    private readonly GoogleBooksSettings _settings;
    private readonly ILogger<GoogleBooksService> _logger;

    public GoogleBooksService(HttpClient httpClient, IOptions<GoogleBooksSettings> settings, ILogger<GoogleBooksService> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<GoogleBooksBookResult> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        var cleanIsbn = isbn.Replace("-", "", StringComparison.Ordinal)
                               .Replace(" ", "", StringComparison.Ordinal)
                               .Trim();

        try
        {

            var url = WithApiKey(_settings.BaseUrl + SearchPath + cleanIsbn);

            _logger.LogInformation("Searching Google Books for ISBN: {Isbn}", cleanIsbn);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Books returned {StatusCode} for ISBN: {Isbn}", response.StatusCode, cleanIsbn);
                return CreateNotFoundResult();
            }

            var searchResponse = await response.Content.ReadFromJsonAsync<GoogleBooksSearchResponse>(
                JsonOptions, cancellationToken);

            if (searchResponse?.Items is null || searchResponse.Items.Count == 0)
            {
                _logger.LogWarning("No items found in Google Books for ISBN: {Isbn}", cleanIsbn);
                return CreateNotFoundResult();
            }

            // Fetch detailed volume info via selfLink for complete metadata
            // The search endpoint often returns truncated titles (e.g. "Fullmetal Alchemist" instead of "Fullmetal Alchemist Tome 23")
            var volumeInfo = await GetDetailedVolumeInfoAsync(searchResponse.Items[0], cancellationToken);

            if (volumeInfo is null)
            {
                _logger.LogWarning("No volumeInfo in Google Books response for ISBN: {Isbn}", cleanIsbn);
                return CreateNotFoundResult();
            }

            // Build cover URL - prefer the largest available image
            var coverUrl = GetBestCoverUrl(volumeInfo.ImageLinks);

            // Publisher is a single string in Google Books, wrap it in a list
            var publishers = string.IsNullOrEmpty(volumeInfo.Publisher)
                ? (IReadOnlyList<string>)[]
                : [volumeInfo.Publisher];

            _logger.LogInformation("Found book: {Title} by {Authors}",
                volumeInfo.Title,
                string.Join(", ", volumeInfo.Authors ?? []));

            return new GoogleBooksBookResult(
                Title: volumeInfo.Title ?? string.Empty,
                Subtitle: volumeInfo.Subtitle,
                Authors: volumeInfo.Authors ?? [],
                Publishers: publishers,
                PublishDate: PublishDateHelper.ParsePublishDate(volumeInfo.PublishedDate, _logger),
                NumberOfPages: volumeInfo.PageCount,
                CoverUrl: coverUrl,
                Description: volumeInfo.Description,
                Categories: volumeInfo.Categories ?? [],
                Language: volumeInfo.Language,
                Found: true
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error searching Google Books for ISBN: {Isbn}", cleanIsbn);
            return CreateNotFoundResult();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for ISBN: {Isbn}", cleanIsbn);
            return CreateNotFoundResult();
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout searching Google Books for ISBN: {Isbn}", cleanIsbn);
            return CreateNotFoundResult();
        }
    }

    private async Task<GoogleBooksVolumeInfo?> GetDetailedVolumeInfoAsync(
        GoogleBooksVolume searchVolume,
        CancellationToken cancellationToken)
    {
        if (searchVolume.SelfLink is not null)
        {
            try
            {
                _logger.LogInformation("Fetching detailed volume info from: {SelfLink}", searchVolume.SelfLink);
                var detailedVolume = await _httpClient.GetFromJsonAsync<GoogleBooksVolume>(
                    WithApiKey(searchVolume.SelfLink), JsonOptions, cancellationToken);

                if (detailedVolume?.VolumeInfo is not null)
                {
                    return detailedVolume.VolumeInfo;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to fetch detailed volume info, using search result data");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse detailed volume info, using search result data");
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Timeout fetching detailed volume info, using search result data");
            }
        }

        // Fallback to search result data
        return searchVolume.VolumeInfo;
    }

    private Uri WithApiKey(string url)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            return new Uri(url);
        }

        var separator = url.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return new Uri($"{url}{separator}key={Uri.EscapeDataString(_settings.ApiKey)}");
    }

    private static Uri? GetBestCoverUrl(GoogleBooksImageLinks? imageLinks)
    {
        if (imageLinks is null)
        {
            return null;
        }

        // Prefer the largest available image
        var url = imageLinks.ExtraLarge
            ?? imageLinks.Large
            ?? imageLinks.Medium
            ?? imageLinks.Small
            ?? imageLinks.Thumbnail
            ?? imageLinks.SmallThumbnail;

        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        // Parse the URL, force HTTPS, and strip the "edge" query parameter to get clean images
        var builder = new UriBuilder(url)
        {
            Scheme = Uri.UriSchemeHttps,
            Port = -1
        };

        var rawQuery = builder.Query.TrimStart('?');
        if (!string.IsNullOrEmpty(rawQuery))
        {
            var filtered = rawQuery
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Where(static param =>
                {
                    var eqIdx = param.IndexOf('=', StringComparison.Ordinal);
                    var key = eqIdx >= 0 ? param[..eqIdx] : param;
                    return !key.Equals("edge", StringComparison.OrdinalIgnoreCase);
                });
            builder.Query = string.Join("&", filtered);
        }

        return new Uri(builder.ToString());
    }

    private static GoogleBooksBookResult CreateNotFoundResult() =>
        new(
            Title: string.Empty,
            Subtitle: null,
            Authors: [],
            Publishers: [],
            PublishDate: null,
            NumberOfPages: null,
            CoverUrl: null,
            Description: null,
            Categories: [],
            Language: null,
            Found: false
        );

    private static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Internal DTOs for JSON deserialization
    private sealed record GoogleBooksSearchResponse(
        [property: JsonPropertyName("items")] IReadOnlyList<GoogleBooksVolume>? Items
    );

    private sealed record GoogleBooksVolume(
        [property: JsonPropertyName("selfLink")] string? SelfLink,
        [property: JsonPropertyName("volumeInfo")] GoogleBooksVolumeInfo? VolumeInfo
    );

    private sealed record GoogleBooksVolumeInfo(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("subtitle")] string? Subtitle,
        [property: JsonPropertyName("authors")] IReadOnlyList<string>? Authors,
        [property: JsonPropertyName("publisher")] string? Publisher,
        [property: JsonPropertyName("publishedDate")] string? PublishedDate,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("pageCount")] int? PageCount,
        [property: JsonPropertyName("categories")] IReadOnlyList<string>? Categories,
        [property: JsonPropertyName("imageLinks")] GoogleBooksImageLinks? ImageLinks,
        [property: JsonPropertyName("language")] string? Language
    );

    private sealed record GoogleBooksImageLinks(
        [property: JsonPropertyName("smallThumbnail")] string? SmallThumbnail,
        [property: JsonPropertyName("thumbnail")] string? Thumbnail,
        [property: JsonPropertyName("small")] string? Small,
        [property: JsonPropertyName("medium")] string? Medium,
        [property: JsonPropertyName("large")] string? Large,
        [property: JsonPropertyName("extraLarge")] string? ExtraLarge
    );
}

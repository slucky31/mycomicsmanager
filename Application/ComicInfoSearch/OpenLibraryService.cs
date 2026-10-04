using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Helpers;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.ComicInfoSearch;

public class OpenLibraryService : IOpenLibraryService
{
    private readonly HttpClient _httpClient;
    private readonly OpenLibrarySettings _settings;
    private readonly ILogger<OpenLibraryService> _logger;

    public OpenLibraryService(HttpClient httpClient, IOptions<OpenLibrarySettings> settings, ILogger<OpenLibraryService> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<OpenLibraryBookResult> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        var cleanIsbn = IsbnHelper.NormalizeIsbn(isbn);

        try
        {

            var url = new Uri(_settings.BaseUrl, $"/isbn/{cleanIsbn}.json");

            _logger.LogInformation("Searching OpenLibrary for ISBN: {Isbn}", cleanIsbn);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenLibrary returned {StatusCode} for ISBN: {Isbn}", response.StatusCode, cleanIsbn);
                return CreateNotFoundResult();
            }

            var bookData = await response.Content.ReadFromJsonAsync<OpenLibraryEdition>(
                JsonOptions, cancellationToken);

            if (bookData is null)
            {
                _logger.LogWarning("Failed to parse OpenLibrary response for ISBN: {Isbn}", cleanIsbn);
                return CreateNotFoundResult();
            }

            // Get author names
            var authors = await GetAuthorNamesAsync(bookData.Authors, cancellationToken);

            // Build cover URL
            Uri? coverUrl = null;
            if (bookData.Covers is { Count: > 0 })
            {
                coverUrl = new Uri(_settings.CoversBaseUrl, $"/b/id/{bookData.Covers[0]}-L.jpg");
            }

            _logger.LogInformation("Found book: {Title} by {Authors}", bookData.Title, string.Join(", ", authors));

            return new OpenLibraryBookResult(
                Title: bookData.Title ?? string.Empty,
                Subtitle: bookData.Subtitle,
                Authors: authors,
                Publishers: bookData.Publishers ?? [],
                PublishDate: PublishDateHelper.ParsePublishDate(bookData.PublishDate, _logger),
                NumberOfPages: bookData.NumberOfPages,
                CoverUrl: coverUrl,
                Found: true
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error searching OpenLibrary for ISBN: {Isbn}", cleanIsbn);
            return CreateNotFoundResult();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for ISBN: {Isbn}", cleanIsbn);
            return CreateNotFoundResult();
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            _logger.LogError(ex, "Timeout searching OpenLibrary for ISBN: {Isbn}", cleanIsbn);
            return CreateNotFoundResult();
        }
    }

    private async Task<IReadOnlyList<string>> GetAuthorNamesAsync(
        IReadOnlyList<OpenLibraryAuthorRef>? authorRefs,
        CancellationToken cancellationToken)
    {
        if (authorRefs is null || authorRefs.Count == 0)
        {
            return [];
        }

        var authorNames = new List<string>();

        foreach (var authorRef in authorRefs)
        {
            try
            {
                var authorUrl = new Uri(_settings.BaseUrl, $"{authorRef.Key}.json");
                var authorData = await _httpClient.GetFromJsonAsync<OpenLibraryAuthor>(
                    authorUrl, JsonOptions, cancellationToken);

                if (authorData?.Name is not null)
                {
                    authorNames.Add(authorData.Name);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "HTTP error fetching author data for key: {AuthorKey}", authorRef.Key);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "JSON error parsing author data for key: {AuthorKey}", authorRef.Key);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Timeout fetching author data for key: {AuthorKey}", authorRef.Key);
            }
        }

        return authorNames;
    }

    private static OpenLibraryBookResult CreateNotFoundResult() =>
        new(
            Title: string.Empty,
            Subtitle: null,
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

    // Internal DTOs for JSON deserialization
    private sealed record OpenLibraryEdition(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("subtitle")] string? Subtitle,
        [property: JsonPropertyName("authors")] IReadOnlyList<OpenLibraryAuthorRef>? Authors,
        [property: JsonPropertyName("publishers")] IReadOnlyList<string>? Publishers,
        [property: JsonPropertyName("publish_date")] string? PublishDate,
        [property: JsonPropertyName("number_of_pages")] int? NumberOfPages,
        [property: JsonPropertyName("covers")] IReadOnlyList<long>? Covers
    );

    private sealed record OpenLibraryAuthorRef(
        [property: JsonPropertyName("key")] string Key
    );

    private sealed record OpenLibraryAuthor(
        [property: JsonPropertyName("name")] string? Name
    );
}

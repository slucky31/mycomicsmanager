using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.ComicInfoSearch;

public class OpenLibraryService : IOpenLibraryService
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<OpenLibraryService>();

    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://openlibrary.org";
    private const string CoversBaseUrl = "https://covers.openlibrary.org";

    public OpenLibraryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OpenLibraryBookResult> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        try
        {
            var cleanIsbn = isbn.Replace("-", "", StringComparison.Ordinal)
                               .Replace(" ", "", StringComparison.Ordinal)
                               .Trim();
            var url = new Uri($"{BaseUrl}/isbn/{cleanIsbn}.json");

            Log.Information("Searching OpenLibrary for ISBN: {Isbn}", cleanIsbn);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("OpenLibrary returned {StatusCode} for ISBN: {Isbn}", response.StatusCode, cleanIsbn);
                return CreateNotFoundResult();
            }

            var bookData = await response.Content.ReadFromJsonAsync<OpenLibraryEdition>(
                JsonOptions, cancellationToken);

            if (bookData is null)
            {
                Log.Warning("Failed to parse OpenLibrary response for ISBN: {Isbn}", cleanIsbn);
                return CreateNotFoundResult();
            }

            // Get author names
            var authors = await GetAuthorNamesAsync(bookData.Authors, cancellationToken);

            // Build cover URL
            Uri? coverUrl = null;
            if (bookData.Covers is { Count: > 0 })
            {
                coverUrl = new Uri($"{CoversBaseUrl}/b/id/{bookData.Covers[0]}-L.jpg");
            }

            Log.Information("Found book: {Title} by {Authors}", bookData.Title, string.Join(", ", authors));

            return new OpenLibraryBookResult(
                Title: bookData.Title ?? string.Empty,
                Subtitle: bookData.Subtitle,
                Authors: authors,
                Publishers: bookData.Publishers ?? [],
                PublishDate: bookData.PublishDate,
                NumberOfPages: bookData.NumberOfPages,
                CoverUrl: coverUrl,
                Found: true
            );
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error searching OpenLibrary for ISBN: {Isbn}", isbn);
            return CreateNotFoundResult();
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "JSON parsing error for ISBN: {Isbn}", isbn);
            return CreateNotFoundResult();
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            Log.Error(ex, "Timeout searching OpenLibrary for ISBN: {Isbn}", isbn);
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
                var authorUrl = $"{BaseUrl}{authorRef.Key}.json";
                var authorData = await _httpClient.GetFromJsonAsync<OpenLibraryAuthor>(
                    authorUrl, JsonOptions, cancellationToken);

                if (authorData?.Name is not null)
                {
                    authorNames.Add(authorData.Name);
                }
            }
            catch (HttpRequestException ex)
            {
                Log.Warning(ex, "HTTP error fetching author data for key: {AuthorKey}", authorRef.Key);
            }
            catch (JsonException ex)
            {
                Log.Warning(ex, "JSON error parsing author data for key: {AuthorKey}", authorRef.Key);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                Log.Warning(ex, "Timeout fetching author data for key: {AuthorKey}", authorRef.Key);
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

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Helpers;
using Application.Interfaces;
using Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.ComicInfoSearch;

public class OpenLibraryService : IOpenLibraryService
{
    private readonly HttpClient _httpClient;
    private readonly OpenLibrarySettings _settings;
    private readonly IFeatureToggles _featureToggles;
    private readonly ILogger<OpenLibraryService> _logger;

    public OpenLibraryService(HttpClient httpClient, IOptions<OpenLibrarySettings> settings, IFeatureToggles featureToggles, ILogger<OpenLibraryService> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _settings = settings.Value;
        _featureToggles = featureToggles;
    }

    public bool IsEnabled => _featureToggles.IsEnabled(FeatureToggle.OpenLibrary);

    public async Task<OpenLibraryBookResult> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        var cleanIsbn = IsbnHelper.NormalizeIsbn(isbn);

        if (!IsEnabled)
        {
            return CreateNotFoundResult();
        }

        return await SearchAsync($"ISBN {cleanIsbn}", async () =>
        {
            var url = new Uri(_settings.BaseUrl, $"/isbn/{cleanIsbn}.json");

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenLibrary returned {StatusCode} for ISBN: {Isbn}", response.StatusCode, cleanIsbn);
                // OpenLibrary answers 404 for a book it does not know.
                return response.StatusCode == HttpStatusCode.NotFound ? CreateNotFoundResult() : CreateFailedResult();
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

            var (serie, volumeNumber) = bookData.Series is { Count: > 0 }
                ? SeriesHelper.ParseSerie(ToComposedForm(bookData.Series[0]))
                : (null as string, null as int?);

            return new OpenLibraryBookResult(
                Title: ToComposedForm(bookData.Title ?? string.Empty),
                Subtitle: bookData.Subtitle is null ? null : ToComposedForm(bookData.Subtitle),
                Authors: authors,
                Publishers: [.. (bookData.Publishers ?? []).Select(ToComposedForm)],
                PublishDate: PublishDateHelper.ParsePublishDate(bookData.PublishDate, _logger),
                NumberOfPages: bookData.NumberOfPages,
                CoverUrl: coverUrl,
                Found: true,
                Serie: serie,
                VolumeNumber: volumeNumber
            );
        }, CreateFailedResult(), cancellationToken);
    }

    // OpenLibrary searches works: a work lists the ISBNs of its editions, the first one stands for it.
    public async Task<BookCandidatesResult> SearchByTextAsync(BookTextQuery query, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || query.IsEmpty)
        {
            return BookCandidatesResult.None;
        }

        var text = query.ToText();
        return await SearchAsync($"\"{text}\"", async () =>
        {
            var url = new Uri(_settings.BaseUrl,
                $"/search.json?q={Uri.EscapeDataString(text)}&fields=title,publisher,first_publish_year,isbn,cover_i&limit={BookCandidatesResult.MaxCandidates}");
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenLibrary returned {StatusCode} for {Query}", response.StatusCode, text);
                return BookCandidatesResult.Failure;
            }

            var search = await response.Content.ReadFromJsonAsync<OpenLibrarySearch>(JsonOptions, cancellationToken);
            return new BookCandidatesResult([.. (search?.Docs ?? [])
                .Where(doc => !string.IsNullOrWhiteSpace(doc.Title))
                .Select(ToCandidate)]).RankedFor(query);
        }, BookCandidatesResult.Failure, cancellationToken);
    }

    private BookCandidate ToCandidate(OpenLibraryWork work)
    {
        var isbns = (work.Isbn ?? []).Select(BookCandidate.ValidIsbn).OfType<string>().ToList();
        return new BookCandidate(
            Title: ToComposedForm(work.Title!),
            Serie: null,
            VolumeNumber: null,
            Publishers: [.. (work.Publisher ?? []).Take(1).Select(ToComposedForm)],
            PublishDate: work.FirstPublishYear is >= 1 and <= 9999 ? new DateOnly(work.FirstPublishYear.Value, 1, 1) : null,
            Isbn: isbns.Find(isbn => isbn.Length == 13) ?? isbns.FirstOrDefault(),
            CoverUrl: work.CoverId is { } coverId ? new Uri(_settings.CoversBaseUrl, $"/b/id/{coverId}-M.jpg") : null);
    }

    private async Task<T> SearchAsync<T>(string searchedFor, Func<Task<T>> search, T failed, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Searching OpenLibrary for {SearchedFor}", searchedFor);
            return await search();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error searching OpenLibrary for {SearchedFor}", searchedFor);
            return failed;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for {SearchedFor}", searchedFor);
            return failed;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            _logger.LogError(ex, "Timeout searching OpenLibrary for {SearchedFor}", searchedFor);
            return failed;
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
                    authorNames.Add(ToComposedForm(authorData.Name));
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

    // OpenLibrary may send "é" as "e" followed by a combining accent: it would not match the same
    // text from another source.
    private static string ToComposedForm(string value) => value.Normalize(NormalizationForm.FormC);

    private static OpenLibraryBookResult CreateFailedResult() => CreateNotFoundResult() with { Failed = true };

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
        [property: JsonPropertyName("covers")] IReadOnlyList<long>? Covers,
        [property: JsonPropertyName("series")] IReadOnlyList<string>? Series
    );

    private sealed record OpenLibrarySearch(
        [property: JsonPropertyName("docs")] IReadOnlyList<OpenLibraryWork>? Docs
    );

    private sealed record OpenLibraryWork(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("publisher")] IReadOnlyList<string>? Publisher,
        [property: JsonPropertyName("first_publish_year")] int? FirstPublishYear,
        [property: JsonPropertyName("isbn")] IReadOnlyList<string>? Isbn,
        [property: JsonPropertyName("cover_i")] long? CoverId
    );

    private sealed record OpenLibraryAuthorRef(
        [property: JsonPropertyName("key")] string Key
    );

    private sealed record OpenLibraryAuthor(
        [property: JsonPropertyName("name")] string? Name
    );
}

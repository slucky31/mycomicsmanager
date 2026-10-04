using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.FeedImports;
using Domain.FeedImports;
using Domain.Primitives;

namespace Web.Infrastructure;

internal sealed class MinifluxClient(HttpClient httpClient) : IMinifluxClient
{
    private const int PageSize = 100;

    private static Serilog.ILogger Log => Serilog.Log.ForContext<MinifluxClient>();

    public async Task<Result<IReadOnlyList<MinifluxCategory>>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await GetAsync<List<CategoryDto>>(new Uri("v1/categories", UriKind.Relative), cancellationToken);
        if (categories.IsFailure)
        {
            return categories.Error!;
        }

        IReadOnlyList<MinifluxCategory> result = categories.Value!
            .Select(c => new MinifluxCategory(c.Id, c.Title ?? string.Empty))
            .ToList();
        return Result<IReadOnlyList<MinifluxCategory>>.Success(result);
    }

    public async Task<Result<IReadOnlyList<MinifluxEntry>>> GetStarredEntriesAsync(long categoryId, CancellationToken cancellationToken = default)
    {
        var entries = new List<MinifluxEntry>();
        while (true)
        {
            var uri = new Uri(string.Create(CultureInfo.InvariantCulture,
                $"v1/entries?starred=true&category_id={categoryId}&order=published_at&direction=asc&limit={PageSize}&offset={entries.Count}"),
                UriKind.Relative);
            var page = await GetAsync<EntriesResponseDto>(uri, cancellationToken);
            if (page.IsFailure)
            {
                return page.Error!;
            }

            var pageEntries = page.Value!.Entries ?? [];
            entries.AddRange(pageEntries.Select(e => new MinifluxEntry(e.Id, e.Title ?? string.Empty, e.Url ?? string.Empty, e.PublishedAt)));

            if (pageEntries.Count < PageSize || entries.Count >= page.Value.Total)
            {
                return Result<IReadOnlyList<MinifluxEntry>>.Success(entries);
            }
        }
    }

    public async Task<Result> UnstarAsync(long entryId, CancellationToken cancellationToken = default)
    {
        var entryUri = new Uri(string.Create(CultureInfo.InvariantCulture, $"v1/entries/{entryId}"), UriKind.Relative);
        var entry = await GetAsync<EntryDto>(entryUri, cancellationToken, notFoundAsNull: true);
        if (entry.IsFailure)
        {
            return entry.Error!;
        }

        // Deleted or already unstarred: toggling the bookmark now would star it again.
        if (entry.Value is null || !entry.Value.Starred)
        {
            return Result.Success();
        }

        var toggleUri = new Uri(string.Create(CultureInfo.InvariantCulture, $"v1/entries/{entryId}/bookmark"), UriKind.Relative);
        try
        {
            using var response = await httpClient.PutAsync(toggleUri, content: null, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("Miniflux returned {StatusCode} when toggling the star of entry {EntryId}", (int)response.StatusCode, entryId);
                return FeedImportError.MinifluxUnavailable;
            }

            return Result.Success();
        }
        catch (Exception ex) when (IsTransportError(ex, cancellationToken))
        {
            Log.Warning(ex, "Miniflux request failed when toggling the star of entry {EntryId}", entryId);
            return FeedImportError.MinifluxUnavailable;
        }
    }

    private async Task<Result<T?>> GetAsync<T>(Uri uri, CancellationToken cancellationToken, bool notFoundAsNull = false)
        where T : class
    {
        try
        {
            using var response = await httpClient.GetAsync(uri, cancellationToken);
            if (notFoundAsNull && response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result<T?>.Success(null);
            }

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("Miniflux returned {StatusCode} for {Uri}", (int)response.StatusCode, uri);
                return response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                    ? FeedImportError.MinifluxUnauthorized
                    : FeedImportError.MinifluxUnavailable;
            }

            var body = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            if (body is null)
            {
                Log.Warning("Miniflux returned an empty body for {Uri}", uri);
                return FeedImportError.MinifluxUnavailable;
            }

            return body;
        }
        catch (Exception ex) when (IsTransportError(ex, cancellationToken))
        {
            Log.Warning(ex, "Miniflux request failed for {Uri}", uri);
            return FeedImportError.MinifluxUnavailable;
        }
    }

    // HttpClient timeouts surface as TaskCanceledException without the caller's token being cancelled.
    private static bool IsTransportError(Exception ex, CancellationToken cancellationToken) =>
        ex is HttpRequestException or JsonException
        || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    private sealed record CategoryDto(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("title")] string? Title);

    private sealed record EntriesResponseDto(
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("entries")] List<EntryDto>? Entries);

#pragma warning disable CA1056 // Raw URL from the Miniflux API
    private sealed record EntryDto(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("published_at")] DateTimeOffset? PublishedAt,
        [property: JsonPropertyName("starred")] bool Starred);
#pragma warning restore CA1056
}

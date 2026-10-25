namespace Application.Interfaces;

// The BnF catalogue has no cover: CoverUrl is always null.
public record BnfBookResult(
    string Title,
    string? Subtitle,
    IReadOnlyList<string> Authors,
    IReadOnlyList<string> Publishers,
    DateOnly? PublishDate,
    int? NumberOfPages,
    Uri? CoverUrl,
    bool Found,
    bool Failed = false,
    string? Serie = null,
    int? VolumeNumber = null
) : IBookSearchResult;

/// <summary>
/// Catalogue of the Bibliothèque nationale de France: every book published in France is
/// registered there (dépôt légal), so it knows most French comics.
/// </summary>
public interface IBnfCatalogueService
{
    // False when the source is turned off (configuration or Settings → Features).
    bool IsEnabled { get; }

    Task<BnfBookResult> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default);

    // The editions whose title or series match the query, for a book without ISBN (#1237).
    Task<BookCandidatesResult> SearchByTextAsync(BookTextQuery query, CancellationToken cancellationToken = default);
}

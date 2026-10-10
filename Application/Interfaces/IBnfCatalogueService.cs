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
    bool Found
) : IBookSearchResult;

/// <summary>
/// Catalogue of the Bibliothèque nationale de France: every book published in France is
/// registered there (dépôt légal), so it knows most French comics.
/// </summary>
public interface IBnfCatalogueService
{
    Task<BnfBookResult> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
}

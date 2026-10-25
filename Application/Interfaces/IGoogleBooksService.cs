namespace Application.Interfaces;

public record GoogleBooksBookResult(
    string Title,
    string? Subtitle,
    IReadOnlyList<string> Authors,
    IReadOnlyList<string> Publishers,
    DateOnly? PublishDate,
    int? NumberOfPages,
    Uri? CoverUrl,
    string? Description,
    IReadOnlyList<string> Categories,
    string? Language,
    bool Found,
    bool Failed = false,
    string? Serie = null,
    int? VolumeNumber = null
) : IBookSearchResult;

public interface IGoogleBooksService
{
    // False when the source is turned off (configuration or Settings → Features).
    bool IsEnabled { get; }

    Task<GoogleBooksBookResult> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default);

    // The editions whose title or series match the query, for a book without ISBN (#1237).
    Task<BookCandidatesResult> SearchByTextAsync(BookTextQuery query, CancellationToken cancellationToken = default);
}

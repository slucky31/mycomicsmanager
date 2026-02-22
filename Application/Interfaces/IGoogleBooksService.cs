namespace Application.Interfaces;

public record GoogleBooksBookResult(
    string Title,
    string? Subtitle,
    IReadOnlyList<string> Authors,
    IReadOnlyList<string> Publishers,
    string? PublishDate,
    int? NumberOfPages,
    Uri? CoverUrl,
    string? Description,
    IReadOnlyList<string> Categories,
    string? Language,
    bool Found
) : IBookSearchResult;

public interface IGoogleBooksService
{
    Task<GoogleBooksBookResult> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
}

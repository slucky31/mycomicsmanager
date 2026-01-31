namespace Application.ComicInfoSearch;

public record OpenLibraryBookResult(
    string Title,
    string? Subtitle,
    IReadOnlyList<string> Authors,
    IReadOnlyList<string> Publishers,
    string? PublishDate,
    int? NumberOfPages,
    Uri? CoverUrl,
    bool Found
);

public interface IOpenLibraryService
{
    Task<OpenLibraryBookResult> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
}

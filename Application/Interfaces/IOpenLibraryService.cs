namespace Application.Interfaces;

public record OpenLibraryBookResult(
    string Title,
    string? Subtitle,
    IReadOnlyList<string> Authors,
    IReadOnlyList<string> Publishers,
    DateOnly? PublishDate,
    int? NumberOfPages,
    Uri? CoverUrl,
    bool Found,
    bool Failed = false
) : IBookSearchResult;

public interface IOpenLibraryService
{
    // False when the source is turned off (configuration or Settings → Features).
    bool IsEnabled { get; }

    Task<OpenLibraryBookResult> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
}

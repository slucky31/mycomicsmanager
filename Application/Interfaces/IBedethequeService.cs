namespace Application.Interfaces;

public record BedethequeBookResult(
    string Title,
    string Serie,
    int VolumeNumber,
    IReadOnlyList<string> Authors,
    IReadOnlyList<string> Publishers,
    DateOnly? PublishDate,
    int? NumberOfPages,
    Uri? CoverUrl,
    bool Found,
    // The source could not be searched (error, timeout, blocked): unlike !Found, it may know the book.
    bool Failed = false
);

public interface IBedethequeService
{
    // False when Bedetheque is turned off in the settings (Bedetheque:Enabled).
    bool IsEnabled { get; }

    Task<BedethequeBookResult> SearchByIsbnAsync(string isbn, CancellationToken ct = default);
}

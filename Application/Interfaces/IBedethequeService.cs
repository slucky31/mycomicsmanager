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
    bool Found
);

public interface IBedethequeService
{
    // False when Bedetheque is turned off in the settings (Bedetheque:Enabled).
    bool IsEnabled { get; }

    Task<BedethequeBookResult> SearchByIsbnAsync(string isbn, CancellationToken ct = default);
}

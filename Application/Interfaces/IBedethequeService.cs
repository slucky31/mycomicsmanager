namespace Application.Interfaces;

// URI parameters/properties should not be strings
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
    Task<BedethequeBookResult> SearchByIsbnAsync(string isbn, CancellationToken ct = default);
}

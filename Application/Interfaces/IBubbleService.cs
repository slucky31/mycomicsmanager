namespace Application.Interfaces;

public record BubbleBookResult(
    string Title,
    string Serie,
    int VolumeNumber,
    IReadOnlyList<string> Authors,
    IReadOnlyList<string> Publishers,
    DateOnly? PublishDate,
    int? NumberOfPages,
    Uri? CoverUrl,
    bool Found,
    bool Failed = false
) : IStructuredBookResult;

public interface IBubbleService
{
    // False when the source is turned off, or when no Algolia key is configured (Bubble:ApiKey).
    bool IsEnabled { get; }

    Task<BubbleBookResult> SearchByIsbnAsync(string isbn, CancellationToken ct = default);
}

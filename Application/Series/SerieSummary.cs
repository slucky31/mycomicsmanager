using Domain.Series;

namespace Application.Series;

// A series of a library as its card shows it (#1205).
public sealed record SerieSummary(
    Guid LibraryId,
    string Name,
    int BookCount,
    string Cover,
    int FirstVolume,
    int LastVolume,
    int? TotalVolumes,
    bool IsFinished,
    SerieCompleteness Completeness)
{
    public bool IsOneShot => Completeness.Status == SerieCompletenessStatus.NotApplicable;
}

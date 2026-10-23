using Domain.Primitives;

namespace Domain.Series;

// What the user knows of a series of a library that its books cannot tell: how many volumes are
// published and whether it is finished (#1205). The series is found by its normalized name.
public sealed class SeriesInfo : Entity<Guid>
{
    public Guid LibraryId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public int? TotalVolumes { get; private set; }

    public bool IsFinished { get; private set; }

    private SeriesInfo() { }

    public static Result<SeriesInfo> Create(Guid libraryId, string name, int? totalVolumes, bool isFinished)
    {
        if (libraryId == Guid.Empty || string.IsNullOrWhiteSpace(name) || name.Trim().Length > SeriesConstants.MaxNameLength)
        {
            return SeriesError.BadRequest;
        }

        var seriesInfo = new SeriesInfo
        {
            Id = Guid.CreateVersion7(),
            LibraryId = libraryId,
            Name = name.Trim(),
            NormalizedName = SerieNames.Normalize(name),
            CreatedOnUtc = DateTime.UtcNow
        };

        var updated = seriesInfo.Update(totalVolumes, isFinished);
        return updated.IsFailure ? updated.Error! : seriesInfo;
    }

    public Result Update(int? totalVolumes, bool isFinished)
    {
        if (totalVolumes is < 1 or > SeriesConstants.MaxTotalVolumes)
        {
            return SeriesError.InvalidTotalVolumes;
        }

        TotalVolumes = totalVolumes;
        IsFinished = isFinished;
        return Result.Success();
    }
}

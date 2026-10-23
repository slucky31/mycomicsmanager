namespace Application.Series.Missing;

// The series of a library that miss volumes (#1205).
public sealed record LibraryMissingVolumes(Guid LibraryId, string LibraryName, IReadOnlyList<SerieSummary> Series)
{
    public int MissingCount => Series.Sum(s => s.Completeness.MissingCount);
}

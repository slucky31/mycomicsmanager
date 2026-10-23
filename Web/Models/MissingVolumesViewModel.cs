using Application.Series;
using Application.Series.Missing;

namespace Web.Models;

// A series that misses volumes, on the Missing volumes page (#1205).
public sealed record MissingSerieViewModel(string Name, string Cover, string Href, string OwnedLabel, IReadOnlyList<string> Missing)
{
    public static MissingSerieViewModel From(SerieSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        var completeness = summary.Completeness;
        return new MissingSerieViewModel(
            summary.Name,
            summary.Cover,
            SerieDisplay.SerieHref(summary.LibraryId, summary.Name),
            $"{completeness.OwnedCount} / {completeness.ExpectedCount}",
            completeness.Missing.Select(SerieDisplay.RangeLabel).ToList());
    }
}

public sealed record MissingLibraryViewModel(Guid LibraryId, string Name, IReadOnlyList<MissingSerieViewModel> Series)
{
    public static MissingLibraryViewModel From(LibraryMissingVolumes library)
    {
        ArgumentNullException.ThrowIfNull(library);
        return new MissingLibraryViewModel(library.LibraryId, library.LibraryName, library.Series.Select(MissingSerieViewModel.From).ToList());
    }
}

public sealed record MissingVolumesViewModel(IReadOnlyList<MissingLibraryViewModel> Libraries, int VolumeCount, int SerieCount)
{
    public static MissingVolumesViewModel From(IReadOnlyList<LibraryMissingVolumes> libraries)
    {
        ArgumentNullException.ThrowIfNull(libraries);
        return new MissingVolumesViewModel(
            libraries.Select(MissingLibraryViewModel.From).ToList(),
            libraries.Sum(l => l.MissingCount),
            libraries.Sum(l => l.Series.Count));
    }

    // "11 volumes in 4 series"; nothing missing says so.
    public string Subtitle => VolumeCount == 0
        ? "Nothing missing"
        : $"{SerieDisplay.VolumeCount(VolumeCount)} in {SerieDisplay.SeriesCount(SerieCount)}";

    // The libraries shown with a library chosen; every one when none (Guid.Empty).
    public IEnumerable<MissingLibraryViewModel> In(Guid libraryId) =>
        libraryId == Guid.Empty ? Libraries : Libraries.Where(l => l.LibraryId == libraryId);
}

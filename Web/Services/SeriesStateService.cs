using Web.Models;

namespace Web.Services;

// The series of a library as they were left (#1366): the filter, and the first series on screen.
public sealed record SeriesPageState(SeriesFilter Filter, int FirstSerie = 0);

// Kept for the time of the visit, like the search of a library: back from a series or a book, the series and the
// volumes of a series open again where they were left.
public sealed class SeriesStateService
{
    private readonly Dictionary<Guid, SeriesPageState> _series = [];
    private readonly Dictionary<(Guid LibraryId, string Name), int> _volumes = [];

    public void SaveSeries(Guid libraryId, SeriesPageState state) => _series[libraryId] = state;

    public SeriesPageState? LoadSeries(Guid libraryId) => _series.GetValueOrDefault(libraryId);

    public void SaveFirstVolume(Guid libraryId, string name, int firstVolume) => _volumes[(libraryId, name)] = firstVolume;

    // The first place of the series on screen when it was left (0: its top).
    public int LoadFirstVolume(Guid libraryId, string name) => _volumes.GetValueOrDefault((libraryId, name));
}

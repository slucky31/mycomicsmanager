namespace Web.Services;

// The view of a library is saved with it (#1210); only the search is kept for the time of the visit.
public sealed record LibraryPageState(string Search);

public sealed class LibraryStateService
{
    private readonly Dictionary<Guid, LibraryPageState> _states = [];

    public void Save(Guid libraryId, LibraryPageState state) =>
        _states[libraryId] = state;

    public LibraryPageState? Load(Guid libraryId) =>
        _states.GetValueOrDefault(libraryId);
}

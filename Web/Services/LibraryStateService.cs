using Web.Enums;

namespace Web.Services;

public sealed record LibraryPageState(string Search, ViewMode View);

public sealed class LibraryStateService
{
    private readonly Dictionary<Guid, LibraryPageState> _states = [];

    public void Save(Guid libraryId, LibraryPageState state) =>
        _states[libraryId] = state;

    public LibraryPageState? Load(Guid libraryId) =>
        _states.GetValueOrDefault(libraryId);
}

using Web.Models;

namespace Web.Services;

// The feeds as they were left (#1386): the filters, the pages loaded, the decision shown in detail and the card to
// bring back on screen (the one whose existing book was opened, otherwise the one shown in detail).
public sealed record FeedImportsPageState(
    FeedImportFilter Filter,
    string? Host,
    string? SearchTerm,
    int Pages,
    Guid? SelectedId,
    Guid? AnchorId);

// Kept for the time of the visit, like the series of a library: back from a book, the feeds open again where they
// were left.
public sealed class FeedImportsStateService
{
    private FeedImportsPageState? _state;

    public void Save(FeedImportsPageState state) => _state = state;

    public FeedImportsPageState? Load() => _state;
}

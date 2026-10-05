namespace Application.Books.MoveTargets;

// Libraries a book can be moved to (same type, not its current one), with the suggested one:
// the library already holding the most books of the same serie.
public sealed record BookMoveTargets(IReadOnlyList<BookMoveTarget> Libraries, Guid? SuggestedLibraryId);

public sealed record BookMoveTarget(Guid Id, string Name, string Color, string Icon, int SameSerieCount);

using Domain.Libraries;

namespace Application.Books.MoveTargets;

// Libraries books can be moved to (same type, not their current one), with the suggested one:
// the library already holding the most books of their series.
public sealed record BookMoveTargets(IReadOnlyList<BookMoveTarget> Libraries, Guid? SuggestedLibraryId)
{
    // The library the book is in, shown apart (it cannot be chosen).
    public BookMoveTarget? Current { get; init; }

    // The type of the book's library, which is the type of every target.
    public LibraryBookType BookType { get; init; }

    // How many books are moved.
    public int BookCount { get; init; } = 1;
}

public sealed record BookMoveTarget(Guid Id, string Name, string Color, string Icon, int SameSerieCount, int BookCount = 0);

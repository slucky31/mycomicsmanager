using Domain.Libraries;

namespace Application.Books.MoveTargets;

// Libraries a book can be moved to (same type, not its current one), with the suggested one:
// the library already holding the most books of the same serie.
public sealed record BookMoveTargets(IReadOnlyList<BookMoveTarget> Libraries, Guid? SuggestedLibraryId)
{
    // The library the book is in, shown apart (it cannot be chosen).
    public BookMoveTarget? Current { get; init; }

    // The type of the book's library, which is the type of every target.
    public LibraryBookType BookType { get; init; }
}

public sealed record BookMoveTarget(Guid Id, string Name, string Color, string Icon, int SameSerieCount, int BookCount = 0);

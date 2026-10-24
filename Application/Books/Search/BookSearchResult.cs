namespace Application.Books.Search;

// The first books found, and how many there are in all.
public sealed record BookSearchResult(
    IReadOnlyList<SerieSearchHit> Series,
    IReadOnlyList<BookSearchHit> Books,
    int BookCount)
{
    public static readonly BookSearchResult Empty = new([], [], 0);
}

// A series of one library whose name holds the term.
public sealed record SerieSearchHit(Guid LibraryId, string Name, int BookCount);

// A book whose title or series holds the term.
public sealed record BookSearchHit(Guid Id, Guid LibraryId, string Title, string Serie, int VolumeNumber, string ImageLink);

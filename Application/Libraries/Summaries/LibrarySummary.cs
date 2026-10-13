using Domain.Libraries;

namespace Application.Libraries.Summaries;

// A library as shown in the navigation and on the libraries page: enough to draw it and link to it,
// with its book count and the covers of its latest books.
public sealed record LibrarySummary(
    Guid Id,
    string Name,
    string Color,
    string Icon,
    LibraryBookType BookType,
    int BookCount)
{
    public const int MaxCovers = 3;

    // The image links of the latest books that have a cover, newest first (at most MaxCovers).
    public IReadOnlyList<string> Covers { get; init; } = [];
}

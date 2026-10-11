using Domain.Libraries;

namespace Application.Libraries.Summaries;

// A library as shown in the navigation: enough to draw it and link to it, with its book count.
public sealed record LibrarySummary(
    Guid Id,
    string Name,
    string Color,
    string Icon,
    LibraryBookType BookType,
    int BookCount);

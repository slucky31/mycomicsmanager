using Domain.Libraries;

namespace Application.Series.Detail;

// A series of a library with its books, by volume (#1205), and the type of the library for the actions of a book (#1220).
public sealed record SerieDetail(string LibraryName, SerieSummary Summary, IReadOnlyList<SerieBookRow> Books, LibraryBookType BookType = LibraryBookType.Physical);

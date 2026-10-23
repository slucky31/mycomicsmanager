namespace Application.Series.Detail;

// A series of a library with its books, by volume (#1205).
public sealed record SerieDetail(string LibraryName, SerieSummary Summary, IReadOnlyList<SerieBookRow> Books);

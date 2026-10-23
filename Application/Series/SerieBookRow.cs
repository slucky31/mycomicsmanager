namespace Application.Series;

// A book as the series pages need it (#1205): where it is, in which series, and what to draw of it.
public sealed record SerieBookRow(
    Guid BookId,
    Guid LibraryId,
    string LibraryName,
    string Serie,
    string Title,
    int VolumeNumber,
    string ImageLink);

namespace Application.Books.Reading;

// A reading date of a book, physical or digital (#1252): a book read twice is listed twice.
public sealed record BookReadingDto(
    Guid ReadingDateId,
    Guid BookId,
    Guid LibraryId,
    string Serie,
    string Title,
    int VolumeNumber,
    string ImageLink,
    DateTime Date,
    int Rating);

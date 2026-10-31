namespace Application.Books.Reading;

// A reading date of a book, physical or digital (#1252): a book read twice is listed twice.
// IsDigital: the book can be read, searched for its ISBN and downloaded from its actions (#1265).
public sealed record BookReadingDto(
    Guid ReadingDateId,
    Guid BookId,
    Guid LibraryId,
    string Serie,
    string Title,
    int VolumeNumber,
    string ImageLink,
    DateTime Date,
    int Rating,
    bool IsDigital);

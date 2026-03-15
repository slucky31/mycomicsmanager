using Domain.Books;

namespace Web.Models;

public sealed record BookListItemViewModel(
    Guid Id,
    string Serie,
    string Title,
    string ISBN,
    int VolumeNumber,
    string ImageLink,
    string Authors,
    string Publishers,
    DateTime? LastRead,
    int LastRating,
    int ReadCount)
{
    public static BookListItemViewModel From(Book book)
    {
        var lastEntry = book.ReadingDates.MaxBy(rd => rd.Date);
        return new(
            book.Id ?? Guid.Empty,
            book.Serie,
            book.Title,
            book.ISBN,
            book.VolumeNumber,
            book.ImageLink,
            book.Authors,
            book.Publishers,
            lastEntry?.Date,
            lastEntry?.Rating ?? 0,
            book.ReadingDates.Count);
    }
}

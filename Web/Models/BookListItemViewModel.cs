using Application.Books.List;
using Domain.Books;

namespace Web.Models;

public sealed record BookListItemViewModel(
    Guid Id,
    string Serie,
    string Title,
    string? ISBN,
    int VolumeNumber,
    string ImageLink,
    string Authors,
    string Publishers,
    DateTime? LastRead,
    int? LastRating,
    int ReadCount,
    IsbnScanState IsbnScanState = IsbnScanState.HasIsbn,
    int IsbnCandidateCount = 0)
{
    public static BookListItemViewModel From(BookSummaryDto dto) =>
        new(dto.Id, dto.Serie, dto.Title, dto.ISBN, dto.VolumeNumber,
            dto.ImageLink, dto.Authors, dto.Publishers,
            dto.LastRead, dto.LastRating, dto.ReadCount, dto.IsbnScanState, dto.IsbnCandidateCount ?? 0);

    public static BookListItemViewModel From(Book book)
    {
        var lastEntry = book.ReadingDates.MaxBy(rd => rd.Date);
        return new(
            book.Id,
            book.Serie,
            book.Title,
            book.ISBN,
            book.VolumeNumber,
            book.ImageLink,
            book.Authors,
            book.Publishers,
            lastEntry?.Date,
            lastEntry?.Rating,
            book.ReadingDates.Count,
            book is DigitalBook digitalBook ? digitalBook.GetIsbnScanState() : IsbnScanState.HasIsbn,
            book is DigitalBook digital ? digital.IsbnCandidates.Count : 0);
    }
}

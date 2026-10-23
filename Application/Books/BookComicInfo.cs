using Application.Interfaces;
using Domain.Books;

namespace Application.Books;

public static class BookComicInfo
{
    // The summary and the penciller are not stored on the book: the ComicInfo.xml keeps its own.
    public static ComicInfoData From(Book book)
    {
        ArgumentNullException.ThrowIfNull(book);

        return new ComicInfoData(
            Title: book.Title,
            Series: book.Serie,
            Number: book.VolumeNumber > 0 ? book.VolumeNumber : null,
            Summary: null,
            Year: book.PublishDate?.Year,
            Month: book.PublishDate?.Month,
            Day: book.PublishDate?.Day,
            Writer: TextOrNull(book.Authors),
            Penciller: null,
            Publisher: TextOrNull(book.Publishers),
            Isbn: TextOrNull(book.ISBN),
            PageCount: book.NumberOfPages);
    }

    private static string? TextOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.Read;

public sealed class GetBookReaderInfoQueryHandler(
    IBookRepository bookRepository,
    IRepository<Library, Guid> libraryRepository,
    IComicPageReader pageReader) : IQueryHandler<GetBookReaderInfoQuery, BookReaderInfoDto>
{
    public async Task<Result<BookReaderInfoDto>> Handle(GetBookReaderInfoQuery request, CancellationToken cancellationToken)
    {
        if (request is null || request.BookId == Guid.Empty || request.UserId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        var bookResult = await OwnedDigitalBook.GetAsync(bookRepository, libraryRepository, request.BookId, request.UserId);
        if (bookResult.IsFailure)
        {
            return bookResult.Error!;
        }

        var book = bookResult.Value!;
        var countResult = await pageReader.CountPagesAsync(book.FilePath, cancellationToken);
        if (countResult.IsFailure)
        {
            return countResult.Error!;
        }

        var pageCount = countResult.Value;
        if (pageCount == 0)
        {
            return BooksError.PageNotFound;
        }

        // The archive may have been replaced by a shorter one since the progress was saved.
        var lastReadPage = Math.Clamp(book.LastReadPage, 0, pageCount - 1);

        return new BookReaderInfoDto(book.Id, book.Serie, book.Title, book.VolumeNumber, pageCount, lastReadPage);
    }
}

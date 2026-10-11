using Application.Abstractions.Messaging;
using Application.Books.Read;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.IsbnScan;

public sealed class ScanBookIsbnCommandHandler(
    IBookRepository bookRepository,
    IRepository<Library, Guid> libraryRepository,
    IIsbnPageScanner scanner,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<ScanBookIsbnCommand, IsbnScanOutcome>
{
    public async Task<Result<IsbnScanOutcome>> Handle(ScanBookIsbnCommand request, CancellationToken cancellationToken)
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
        if (!string.IsNullOrWhiteSpace(book.ISBN))
        {
            return BooksError.BadRequest;
        }

        return await BookIsbnScan.ScanAndSaveAsync(book, scanner, bookRepository, unitOfWork, clock, cancellationToken);
    }
}

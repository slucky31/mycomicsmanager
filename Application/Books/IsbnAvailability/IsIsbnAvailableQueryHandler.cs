using Application.Abstractions.Messaging;
using Application.Helpers;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.IsbnAvailability;

public sealed class IsIsbnAvailableQueryHandler(
    IBookRepository bookRepository,
    IRepository<Library, Guid> libraryRepository) : IQueryHandler<IsIsbnAvailableQuery, bool>
{
    public async Task<Result<bool>> Handle(IsIsbnAvailableQuery query, CancellationToken cancellationToken)
    {
        if (query is null || query.BookId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        if (string.IsNullOrWhiteSpace(query.Isbn) || !IsbnHelper.IsValidISBN(query.Isbn))
        {
            return BooksError.InvalidISBN;
        }

        var book = await bookRepository.GetByIdAsync(query.BookId);
        if (book is null)
        {
            return BooksError.NotFound;
        }

        var library = await libraryRepository.GetByIdAsync(book.LibraryId);
        if (library is null || library.UserId != query.UserId)
        {
            return BooksError.NotFound;
        }

        var existingBook = await bookRepository.GetByIsbnAsync(IsbnHelper.NormalizeIsbn(query.Isbn), book.LibraryId, cancellationToken);
        return existingBook is null || existingBook.Id == book.Id;
    }
}

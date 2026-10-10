using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.Read;

internal static class OwnedDigitalBook
{
    // A book of another user is reported as not found, so its existence is not disclosed.
    public static async Task<Result<DigitalBook>> GetAsync(
        IBookRepository bookRepository,
        IRepository<Library, Guid> libraryRepository,
        Guid bookId,
        Guid userId)
    {
        var book = await bookRepository.GetByIdAsync(bookId);
        if (book is null)
        {
            return BooksError.NotFound;
        }

        var library = await libraryRepository.GetByIdAsync(book.LibraryId);
        if (library is null || library.UserId != userId)
        {
            return BooksError.NotFound;
        }

        if (book is not DigitalBook digitalBook)
        {
            return BooksError.NotDigital;
        }

        return digitalBook;
    }
}

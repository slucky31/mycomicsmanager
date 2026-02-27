using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.GetById;

public sealed class GetBookQueryHandler(
    IBookRepository bookRepository,
    IRepository<Library, Guid> libraryRepository) : IQueryHandler<GetBookByIdQuery, Book>
{
    public async Task<Result<Book>> Handle(GetBookByIdQuery request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BooksError.BadRequest;
        }

        if (request.Id == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        var book = await bookRepository.GetByIdAsync(request.Id);
        if (book is null)
        {
            return BooksError.NotFound;
        }

        if (request.UserId != Guid.Empty)
        {
            var library = await libraryRepository.GetByIdAsync(book.LibraryId);
            if (library is null || library.UserId != request.UserId)
            {
                return BooksError.NotFound;
            }
        }

        return book;
    }
}

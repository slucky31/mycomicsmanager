using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;

namespace Application.Books.GetById;

public sealed class GetBookQueryHandler(IBookRepository bookRepository) : IQueryHandler<GetBookByIdQuery, Book>
{
    public async Task<Result<Book>> Handle(GetBookByIdQuery request)
    {
        Guard.Against.Null(request);

        if (request.Id == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        var book = await bookRepository.GetByIdAsync(request.Id);
        if (book is null)
        {
            return BooksError.NotFound;
        }

        return book;
    }
}

using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;

namespace Application.Books.GetById;

public sealed class GetBookQueryHandler(IRepository<Book, Guid> bookRepository) : IQueryHandler<GetBookQuery, Book>
{
    public async Task<Result<Book>> Handle(GetBookQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        var book = await bookRepository.GetByIdAsync(request.Id);
        if (book == null)
        {
            return BooksError.NotFound;
        }

        return book;
    }
}
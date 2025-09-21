using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;

namespace Application.Books.List;

public sealed class GetBooksQueryHandler(IRepository<Book, Guid> bookRepository) : IQueryHandler<GetBooksQuery, List<Book>>
{
    public async Task<Result<List<Book>>> Handle(GetBooksQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        var books = await bookRepository.ListAsync();
        return books;
    }
}
using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Primitives;

namespace Application.Books.Covers;

public sealed class CountCoversToMoveQueryHandler(IBookRepository bookRepository, BookCoverStore coverStore)
    : IQueryHandler<CountCoversToMoveQuery, int>
{
    public async Task<Result<int>> Handle(CountCoversToMoveQuery query, CancellationToken cancellationToken)
    {
        var books = await bookRepository.ListAsync(cancellationToken);
        return books.Count(coverStore.IsShared);
    }
}

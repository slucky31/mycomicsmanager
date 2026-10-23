using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;

namespace Application.Books.ArchiveRefresh;

public sealed class StartArchiveRefreshCommandHandler(
    IBookRepository bookRepository,
    IArchiveRefreshJobEnqueuer enqueuer) : ICommandHandler<StartArchiveRefreshCommand, int>
{
    public async Task<Result<int>> Handle(StartArchiveRefreshCommand request, CancellationToken cancellationToken)
    {
        if (request is null || request.UserId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        var books = await bookRepository.ListByUserIdAsync(request.UserId, cancellationToken);
        var count = books.Count(b => b is DigitalBook);
        if (count > 0)
        {
            enqueuer.Enqueue(request.UserId);
        }

        return count;
    }
}

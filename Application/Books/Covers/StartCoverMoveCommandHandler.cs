using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Primitives;

namespace Application.Books.Covers;

public sealed class StartCoverMoveCommandHandler(
    IBookRepository bookRepository,
    BookCoverStore coverStore,
    ICoverMoveJobEnqueuer enqueuer) : ICommandHandler<StartCoverMoveCommand, int>
{
    public async Task<Result<int>> Handle(StartCoverMoveCommand request, CancellationToken cancellationToken)
    {
        var books = await bookRepository.ListAsync(cancellationToken);
        var count = books.Count(coverStore.NeedsCopy);
        if (count > 0)
        {
            enqueuer.Enqueue();
        }

        return count;
    }
}

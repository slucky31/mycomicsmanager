using Application.Abstractions.Messaging;
using Application.Books.Move;
using Application.Books.MoveTargets;
using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;

namespace Web.Services;

public class BookMoveService(
    IQueryHandler<GetBookMoveTargetsQuery, BookMoveTargets> getTargetsHandler,
    ICommandHandler<MoveBookCommand, Book> moveHandler,
    ICurrentUserService currentUserService) : IBookMoveService
{
    public async Task<Result<BookMoveTargets>> GetTargetsAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await getTargetsHandler.Handle(new GetBookMoveTargetsQuery(bookId, userIdResult.Value), cancellationToken);
    }

    public async Task<Result<Book>> MoveAsync(Guid bookId, Guid targetLibraryId, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await moveHandler.Handle(new MoveBookCommand(bookId, targetLibraryId, userIdResult.Value), cancellationToken);
    }
}

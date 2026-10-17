using Application.Abstractions.Messaging;
using Application.Books.MoveTargets;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Application.Books.Move;

// Moves the books one by one, each with its own checks and save (MoveBookCommandHandler): a book that cannot be
// moved stays where it is and the others are still moved.
public sealed class MoveBooksCommandHandler(
    ICommandHandler<MoveBookCommand, Book> moveBookHandler,
    ILogger<MoveBooksCommandHandler> logger) : ICommandHandler<MoveBooksCommand, MoveBooksResult>
{
    public async Task<Result<MoveBooksResult>> Handle(MoveBooksCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);
        if (request.BookIds is null || request.BookIds.Count == 0 || request.BookIds.Count > GetBookMoveTargetsQueryHandler.MaxBooks
            || request.BookIds.Contains(Guid.Empty) || request.TargetLibraryId == Guid.Empty || request.UserId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        var moved = 0;
        TError? firstError = null;
        var bookIds = request.BookIds.Distinct().ToList();
        foreach (var bookId in bookIds)
        {
            var result = await moveBookHandler.Handle(new MoveBookCommand(bookId, request.TargetLibraryId, request.UserId), cancellationToken);
            if (result.IsSuccess)
            {
                moved++;
                continue;
            }

            firstError ??= result.Error;
            logger.LogWarning("Book {BookId} was not moved to library {LibraryId}: [{Code}] {Description}",
                bookId, request.TargetLibraryId, result.Error!.Code, result.Error.Description);
        }

        return new MoveBooksResult(moved, bookIds.Count - moved, firstError);
    }
}

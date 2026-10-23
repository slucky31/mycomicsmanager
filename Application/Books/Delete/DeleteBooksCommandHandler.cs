using Application.Abstractions.Messaging;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Application.Books.Delete;

// Deletes the books one by one, each with its own checks and save (DeleteBookCommandHandler): a book that cannot be
// deleted stays and the others are still deleted.
public sealed class DeleteBooksCommandHandler(
    ICommandHandler<DeleteBookCommand> deleteBookHandler,
    ILogger<DeleteBooksCommandHandler> logger) : ICommandHandler<DeleteBooksCommand, DeleteBooksResult>
{
    // The same limit as the move of several books.
    public const int MaxBooks = 500;

    public async Task<Result<DeleteBooksResult>> Handle(DeleteBooksCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);
        if (request.BookIds is null || request.BookIds.Count == 0 || request.BookIds.Count > MaxBooks
            || request.BookIds.Contains(Guid.Empty) || request.UserId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        var deleted = 0;
        TError? firstError = null;
        var bookIds = request.BookIds.Distinct().ToList();
        foreach (var bookId in bookIds)
        {
            var result = await deleteBookHandler.Handle(new DeleteBookCommand(bookId, request.UserId), cancellationToken);
            if (result.IsSuccess)
            {
                deleted++;
                continue;
            }

            firstError ??= result.Error;
            logger.LogWarning("Book {BookId} was not deleted: [{Code}] {Description}",
                bookId, result.Error!.Code, result.Error.Description);
        }

        return new DeleteBooksResult(deleted, bookIds.Count - deleted, firstError);
    }
}

using Application.Abstractions.Messaging;
using Application.Books.Read;
using Application.Books.ReadingProgress;
using Application.Interfaces;
using Domain.Primitives;

namespace Web.Services;

public class BookReaderService(
    IQueryHandler<GetBookReaderInfoQuery, BookReaderInfoDto> getReaderInfoHandler,
    ICommandHandler<UpdateReadingProgressCommand> updateProgressHandler,
    ICurrentUserService currentUserService) : IBookReaderService
{
    public async Task<Result<BookReaderInfoDto>> GetReaderInfoAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await getReaderInfoHandler.Handle(new GetBookReaderInfoQuery(bookId, userIdResult.Value), cancellationToken);
    }

    public async Task<Result> SaveProgressAsync(Guid bookId, int pageIndex, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await updateProgressHandler.Handle(new UpdateReadingProgressCommand(bookId, userIdResult.Value, pageIndex), cancellationToken);
    }
}

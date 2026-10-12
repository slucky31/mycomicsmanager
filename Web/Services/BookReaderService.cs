using Application.Abstractions.Messaging;
using Application.Books.Read;
using Application.Books.ReaderDisplay;
using Application.Books.ReadingProgress;
using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;

namespace Web.Services;

public class BookReaderService(
    IQueryHandler<GetBookReaderInfoQuery, BookReaderInfoDto> getReaderInfoHandler,
    ICommandHandler<UpdateReadingProgressCommand> updateProgressHandler,
    ICommandHandler<UpdateReaderSettingsCommand> updateSettingsHandler,
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

    public async Task<Result> SaveSettingsAsync(Guid bookId, ReaderSettings settings, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await updateSettingsHandler.Handle(new UpdateReaderSettingsCommand(bookId, userIdResult.Value, settings), cancellationToken);
    }
}

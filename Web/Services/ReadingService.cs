using Application.Abstractions.Messaging;
using Application.Books.Reading;
using Application.Interfaces;
using Domain.Primitives;

namespace Web.Services;

public class ReadingService(
    IQueryHandler<GetBooksInProgressQuery, IReadOnlyList<BookInProgressDto>> getBooksInProgressHandler,
    IQueryHandler<GetRecentReadingsQuery, IPagedList<BookReadingDto>> getRecentReadingsHandler,
    ICurrentUserService currentUserService) : IReadingService
{
    public async Task<Result<IReadOnlyList<BookInProgressDto>>> GetInProgress(CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await getBooksInProgressHandler.Handle(new GetBooksInProgressQuery(userIdResult.Value), cancellationToken);
    }

    public async Task<Result<IPagedList<BookReadingDto>>> GetRecentReadings(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await getRecentReadingsHandler.Handle(new GetRecentReadingsQuery(userIdResult.Value, page, pageSize), cancellationToken);
    }
}

using Application.Abstractions.Messaging;
using Application.Books.ComicInfoSync;
using Application.Interfaces;
using Domain.Primitives;

namespace Web.Services;

public class ComicInfoSyncService(
    ICommandHandler<StartComicInfoSyncCommand, int> startSyncHandler,
    ICurrentUserService currentUserService) : IComicInfoSyncService
{
    public async Task<Result<int>> StartAsync(CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await startSyncHandler.Handle(new StartComicInfoSyncCommand(userIdResult.Value), cancellationToken);
    }
}

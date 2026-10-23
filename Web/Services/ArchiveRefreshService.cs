using Application.Abstractions.Messaging;
using Application.Books.ArchiveRefresh;
using Application.Interfaces;
using Domain.Primitives;

namespace Web.Services;

public class ArchiveRefreshService(
    ICommandHandler<StartArchiveRefreshCommand, int> startRefreshHandler,
    ICurrentUserService currentUserService) : IArchiveRefreshService
{
    public async Task<Result<int>> StartAsync(CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await startRefreshHandler.Handle(new StartArchiveRefreshCommand(userIdResult.Value), cancellationToken);
    }
}

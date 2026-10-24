using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Users.HiddenLibraries;
using Domain.Primitives;

namespace Web.Services;

public class HiddenLibrariesService(
    IQueryHandler<GetShowHiddenLibrariesQuery, bool> getShowHiddenLibrariesHandler,
    ICommandHandler<SetShowHiddenLibrariesCommand> setShowHiddenLibrariesHandler,
    ICurrentUserService currentUserService) : IHiddenLibrariesService
{
    public async Task<Result<bool>> GetShowHiddenLibraries(CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await getShowHiddenLibrariesHandler.Handle(new GetShowHiddenLibrariesQuery(userIdResult.Value), cancellationToken);
    }

    public async Task<Result> SetShowHiddenLibraries(bool show, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await setShowHiddenLibrariesHandler.Handle(new SetShowHiddenLibrariesCommand(userIdResult.Value, show), cancellationToken);
    }
}

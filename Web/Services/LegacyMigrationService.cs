using Application.Abstractions.Messaging;
using Application.ImportJobs.MigrateLegacy;
using Application.Interfaces;
using Domain.Primitives;

namespace Web.Services;

public class LegacyMigrationService(
    ICommandHandler<StartLegacyMigrationCommand, int> startMigrationHandler,
    ICurrentUserService currentUserService) : ILegacyMigrationService
{
    public async Task<Result<int>> StartAsync(int count, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await startMigrationHandler.Handle(new StartLegacyMigrationCommand(count, userIdResult.Value), cancellationToken);
    }
}

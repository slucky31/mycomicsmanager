using Application.Abstractions.Messaging;
using Application.ImportJobs.MigrateLegacy;
using Application.Interfaces;
using Domain.Primitives;

namespace Web.Services;

public class LegacyMigrationService(
    ICommandHandler<MigrateLegacyBooksCommand, MigrateLegacyBooksResult> migrateHandler,
    ICurrentUserService currentUserService) : ILegacyMigrationService
{
    public async Task<Result<MigrateLegacyBooksResult>> MigrateAsync(int count, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await migrateHandler.Handle(new MigrateLegacyBooksCommand(count, userIdResult.Value), cancellationToken);
    }
}

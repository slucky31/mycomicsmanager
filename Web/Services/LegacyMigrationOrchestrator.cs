using Application.Abstractions.Messaging;
using Application.ImportJobs.MigrateLegacy;
using Application.Interfaces;
using Hangfire;

namespace Web.Services;

public class LegacyMigrationOrchestrator(IServiceScopeFactory scopeFactory, ILogger<LegacyMigrationOrchestrator> logger) : ILegacyMigrationOrchestrator
{
    // Two batches at once would pick the same first files. A file move is not retried: what is left stays in the old library.
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    [AutomaticRetry(Attempts = 0)]
    public async Task MigrateAsync(int count, Guid userId, CancellationToken ct = default)
    {
        logger.LogInformation("Legacy migration of {Count} file(s) for user {UserId} started", count, userId);
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<MigrateLegacyBooksCommand, MigrateLegacyBooksResult>>();

        var result = await handler.Handle(new MigrateLegacyBooksCommand(count, userId), ct);

        if (result.IsFailure)
        {
            logger.LogError("Legacy migration for user {UserId} failed: [{Code}] {Description}",
                userId, result.Error!.Code, result.Error.Description);
        }
        else
        {
            logger.LogInformation("Legacy migration for user {UserId} completed: {Moved} moved, {Skipped} skipped, {Failed} failed, {Remaining} left",
                userId, result.Value!.Moved, result.Value.Skipped, result.Value.Failed, result.Value.Remaining);
        }
    }
}

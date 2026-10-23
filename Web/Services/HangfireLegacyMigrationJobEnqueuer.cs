using Application.Interfaces;
using Hangfire;

namespace Web.Services;

public class HangfireLegacyMigrationJobEnqueuer : ILegacyMigrationJobEnqueuer
{
    public string Enqueue(int count, Guid userId)
        => BackgroundJob.Enqueue<ILegacyMigrationOrchestrator>(x => x.MigrateAsync(count, userId, CancellationToken.None));
}

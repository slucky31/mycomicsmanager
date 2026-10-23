using Application.Interfaces;
using Hangfire;

namespace Web.Services;

public class HangfireComicInfoSyncJobEnqueuer : IComicInfoSyncJobEnqueuer
{
    public string Enqueue(Guid userId)
        => BackgroundJob.Enqueue<IComicInfoSyncOrchestrator>(x => x.SyncAsync(userId, CancellationToken.None));
}

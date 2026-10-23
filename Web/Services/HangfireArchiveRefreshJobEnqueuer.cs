using Application.Interfaces;
using Hangfire;

namespace Web.Services;

public class HangfireArchiveRefreshJobEnqueuer : IArchiveRefreshJobEnqueuer
{
    public string Enqueue(Guid userId)
        => BackgroundJob.Enqueue<IArchiveRefreshOrchestrator>(x => x.RefreshAsync(userId, CancellationToken.None));
}

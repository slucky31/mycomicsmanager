using Application.Interfaces;
using Hangfire;

namespace Web.Services;

public class HangfireCoverMoveJobEnqueuer : ICoverMoveJobEnqueuer
{
    public string Enqueue()
        => BackgroundJob.Enqueue<ICoverMoveOrchestrator>(x => x.MoveAsync(CancellationToken.None));
}

using Application.Interfaces;
using Hangfire;

namespace Web.Services;

public class HangfireImportJobEnqueuer : IImportJobEnqueuer
{
    public string Enqueue(Guid importJobId)
        => BackgroundJob.Enqueue<IImportOrchestrator>(x => x.ProcessAsync(importJobId, CancellationToken.None));
}

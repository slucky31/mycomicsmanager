using Application.Interfaces;
using Hangfire;

namespace Web.Services;

public class HangfireIsbnScanJobEnqueuer : IIsbnScanJobEnqueuer
{
    public string Enqueue(Guid libraryId)
        => BackgroundJob.Enqueue<IIsbnScanOrchestrator>(x => x.ScanLibraryAsync(libraryId, CancellationToken.None));
}

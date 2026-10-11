using Application.Abstractions.Messaging;
using Application.Books.IsbnScan;
using Application.Interfaces;
using Hangfire;

namespace Web.Services;

public class IsbnScanOrchestrator(IServiceScopeFactory scopeFactory, ILogger<IsbnScanOrchestrator> logger) : IIsbnScanOrchestrator
{
    // Scanning the same library twice at once would only read the same pages twice.
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task ScanLibraryAsync(Guid libraryId, CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ScanLibraryIsbnsCommand, LibraryIsbnScanSummary>>();

        var result = await handler.Handle(new ScanLibraryIsbnsCommand(libraryId), ct);

        if (result.IsFailure)
        {
            logger.LogError("ISBN scan of library {LibraryId} failed: [{Code}] {Description}",
                libraryId, result.Error!.Code, result.Error.Description);
        }
        else
        {
            var summary = result.Value!;
            logger.LogInformation(
                "ISBN scan of library {LibraryId} completed: {Assigned} assigned, {WithCandidates} to pick, {WithoutIsbn} without ISBN, {NotScanned} not scanned",
                libraryId, summary.Assigned, summary.WithCandidates, summary.WithoutIsbn, summary.NotScanned);
        }
    }
}

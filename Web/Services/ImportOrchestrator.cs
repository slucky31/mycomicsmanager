using Application.Abstractions.Messaging;
using Application.ImportJobs.Process;
using Application.Interfaces;

namespace Web.Services;

public class ImportOrchestrator(IServiceScopeFactory scopeFactory, ILogger<ImportOrchestrator> logger) : IImportOrchestrator
{
    public async Task ProcessAsync(Guid importJobId, CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ProcessImportJobCommand, ImportJobOutcome>>();

        var result = await handler.Handle(new ProcessImportJobCommand(importJobId), ct);

        if (result.IsFailure)
        {
            logger.LogError("Import job {ImportJobId} failed: [{Code}] {Description}",
                importJobId, result.Error!.Code, result.Error.Description);
        }
        else if (result.Value!.Book is { } book)
        {
            logger.LogInformation("Import job {ImportJobId} completed. DigitalBook {BookId} created.",
                importJobId, book.Id);
        }
        else
        {
            logger.LogInformation("Import job {ImportJobId} split into {Count} imports.",
                importJobId, result.Value.SplitFileNames.Count);
        }
    }
}

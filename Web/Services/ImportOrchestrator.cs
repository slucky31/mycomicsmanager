using Application.Abstractions.Messaging;
using Application.ImportJobs.Process;
using Application.Interfaces;
using Domain.Books;

namespace Web.Services;

public class ImportOrchestrator(IServiceScopeFactory scopeFactory, ILogger<ImportOrchestrator> logger) : IImportOrchestrator
{
    public async Task ProcessAsync(Guid importJobId, CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ProcessImportJobCommand, DigitalBook>>();

        var result = await handler.Handle(new ProcessImportJobCommand(importJobId), ct);

        if (result.IsFailure)
        {
            logger.LogError("Import job {ImportJobId} failed: [{Code}] {Description}",
                importJobId, result.Error!.Code, result.Error.Description);
        }
        else
        {
            logger.LogInformation("Import job {ImportJobId} completed. DigitalBook {BookId} created.",
                importJobId, result.Value!.Id);
        }
    }
}

using Application.Abstractions.Messaging;
using Application.ImportJobs.Process;
using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence.Services;

public class ImportOrchestrator(IServiceScopeFactory scopeFactory) : IImportOrchestrator
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<ImportOrchestrator>();

    public async Task ProcessAsync(Guid importJobId, CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ProcessImportJobCommand, DigitalBook>>();

        Result<DigitalBook> result;
        try
        {
            result = await handler.Handle(new ProcessImportJobCommand(importJobId), ct);
        }
#pragma warning disable CA1031 // Hangfire needs to catch all exceptions to retry
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception processing import job {ImportJobId}", importJobId);
            throw;
        }
#pragma warning restore CA1031

        if (result.IsFailure)
        {
            Log.Error("Import job {ImportJobId} failed: [{Code}] {Description}",
                importJobId, result.Error!.Code, result.Error.Description);
        }
        else
        {
            Log.Information("Import job {ImportJobId} completed. DigitalBook {BookId} created.",
                importJobId, result.Value!.Id);
        }
    }
}

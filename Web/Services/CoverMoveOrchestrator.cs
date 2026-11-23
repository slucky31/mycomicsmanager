using Application.Abstractions.Messaging;
using Application.Books.Covers;
using Application.Interfaces;
using Hangfire;

namespace Web.Services;

public class CoverMoveOrchestrator(IServiceScopeFactory scopeFactory, ILogger<CoverMoveOrchestrator> logger) : ICoverMoveOrchestrator
{
    // Copying the same covers twice at once would only upload them twice.
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task MoveAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Move of the covers to their own image started");
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<MoveCoversCommand, CoverMoveSummary>>();

        var result = await handler.Handle(new MoveCoversCommand(), ct);

        if (result.IsFailure)
        {
            logger.LogError("Move of the covers failed: [{Code}] {Description}", result.Error!.Code, result.Error.Description);
        }
        else
        {
            logger.LogInformation("Move of the covers completed: {Moved} moved, {Failed} failed", result.Value!.Moved, result.Value.Failed);
        }
    }
}

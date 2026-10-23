using Application.Abstractions.Messaging;
using Application.Books.ComicInfoSync;
using Application.Interfaces;
using Hangfire;

namespace Web.Services;

public class ComicInfoSyncOrchestrator(IServiceScopeFactory scopeFactory, ILogger<ComicInfoSyncOrchestrator> logger) : IComicInfoSyncOrchestrator
{
    // Writing the same archives twice at once would only copy them twice.
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task SyncAsync(Guid userId, CancellationToken ct = default)
    {
        logger.LogInformation("ComicInfo.xml sync of user {UserId} started", userId);
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SyncComicInfoCommand, ComicInfoSyncSummary>>();

        var result = await handler.Handle(new SyncComicInfoCommand(userId), ct);

        if (result.IsFailure)
        {
            logger.LogError("ComicInfo.xml sync of user {UserId} failed: [{Code}] {Description}",
                userId, result.Error!.Code, result.Error.Description);
        }
        else
        {
            logger.LogInformation("ComicInfo.xml sync of user {UserId} completed: {Updated} written, {Failed} failed",
                userId, result.Value!.Updated, result.Value.Failed);
        }
    }
}

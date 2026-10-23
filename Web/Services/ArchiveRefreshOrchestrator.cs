using Application.Abstractions.Messaging;
using Application.Books.ArchiveRefresh;
using Application.Interfaces;
using Hangfire;

namespace Web.Services;

public class ArchiveRefreshOrchestrator(IServiceScopeFactory scopeFactory, ILogger<ArchiveRefreshOrchestrator> logger) : IArchiveRefreshOrchestrator
{
    // Reading the same archives twice at once would only upload the same covers twice.
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task RefreshAsync(Guid userId, CancellationToken ct = default)
    {
        logger.LogInformation("Cover and pages refresh of user {UserId} started", userId);
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<RefreshFromArchiveCommand, ArchiveRefreshSummary>>();

        var result = await handler.Handle(new RefreshFromArchiveCommand(userId), ct);

        if (result.IsFailure)
        {
            logger.LogError("Cover and pages refresh of user {UserId} failed: [{Code}] {Description}",
                userId, result.Error!.Code, result.Error.Description);
        }
        else
        {
            logger.LogInformation("Cover and pages refresh of user {UserId} completed: {Updated} updated, {Unchanged} unchanged, {Failed} failed",
                userId, result.Value!.Updated, result.Value.Unchanged, result.Value.Failed);
        }
    }
}

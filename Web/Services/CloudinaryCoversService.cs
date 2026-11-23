using Application.Abstractions.Messaging;
using Application.Books.Covers;
using Domain.Primitives;

namespace Web.Services;

// Each call runs in its own scope, with its own DbContext: the card loads while the Settings page queries the
// DbContext of the circuit, and two queries on the same DbContext at once fail.
public class CloudinaryCoversService(IServiceScopeFactory scopeFactory) : ICloudinaryCoversService
{
    public Task<Result<int>> CountToMoveAsync(CancellationToken cancellationToken = default) =>
        RunAsync<IQueryHandler<CountCoversToMoveQuery, int>, Result<int>>(
            h => h.Handle(new CountCoversToMoveQuery(), cancellationToken));

    public Task<Result<int>> StartMoveAsync(CancellationToken cancellationToken = default) =>
        RunAsync<ICommandHandler<StartCoverMoveCommand, int>, Result<int>>(
            h => h.Handle(new StartCoverMoveCommand(), cancellationToken));

    public Task<Result<UnusedCovers>> FindUnusedAsync(CancellationToken cancellationToken = default) =>
        RunAsync<IQueryHandler<FindUnusedCoversQuery, UnusedCovers>, Result<UnusedCovers>>(
            h => h.Handle(new FindUnusedCoversQuery(), cancellationToken));

    public Task<Result<DeletedCovers>> DeleteUnusedAsync(IReadOnlyList<string> publicIds, CancellationToken cancellationToken = default) =>
        RunAsync<ICommandHandler<DeleteUnusedCoversCommand, DeletedCovers>, Result<DeletedCovers>>(
            h => h.Handle(new DeleteUnusedCoversCommand(publicIds), cancellationToken));

    private async Task<TResult> RunAsync<THandler, TResult>(Func<THandler, Task<TResult>> handle)
        where THandler : notnull
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await handle(scope.ServiceProvider.GetRequiredService<THandler>());
    }
}

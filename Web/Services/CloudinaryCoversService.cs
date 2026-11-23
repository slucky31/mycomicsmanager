using Application.Abstractions.Messaging;
using Application.Books.Covers;
using Domain.Primitives;

namespace Web.Services;

public class CloudinaryCoversService(
    IQueryHandler<CountCoversToMoveQuery, int> countHandler,
    ICommandHandler<StartCoverMoveCommand, int> startMoveHandler,
    IQueryHandler<FindUnusedCoversQuery, UnusedCovers> findUnusedHandler,
    ICommandHandler<DeleteUnusedCoversCommand, DeletedCovers> deleteUnusedHandler) : ICloudinaryCoversService
{
    public Task<Result<int>> CountToMoveAsync(CancellationToken cancellationToken = default) =>
        countHandler.Handle(new CountCoversToMoveQuery(), cancellationToken);

    public Task<Result<int>> StartMoveAsync(CancellationToken cancellationToken = default) =>
        startMoveHandler.Handle(new StartCoverMoveCommand(), cancellationToken);

    public Task<Result<UnusedCovers>> FindUnusedAsync(CancellationToken cancellationToken = default) =>
        findUnusedHandler.Handle(new FindUnusedCoversQuery(), cancellationToken);

    public Task<Result<DeletedCovers>> DeleteUnusedAsync(IReadOnlyList<string> publicIds, CancellationToken cancellationToken = default) =>
        deleteUnusedHandler.Handle(new DeleteUnusedCoversCommand(publicIds), cancellationToken);
}

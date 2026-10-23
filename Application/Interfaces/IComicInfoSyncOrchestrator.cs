namespace Application.Interfaces;

public interface IComicInfoSyncOrchestrator
{
    Task SyncAsync(Guid userId, CancellationToken ct = default);
}

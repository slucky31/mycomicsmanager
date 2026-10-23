namespace Application.Interfaces;

public interface IArchiveRefreshOrchestrator
{
    Task RefreshAsync(Guid userId, CancellationToken ct = default);
}

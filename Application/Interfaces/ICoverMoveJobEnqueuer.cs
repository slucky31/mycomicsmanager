namespace Application.Interfaces;

public interface ICoverMoveJobEnqueuer
{
    string Enqueue();
}

public interface ICoverMoveOrchestrator
{
    Task MoveAsync(CancellationToken ct = default);
}

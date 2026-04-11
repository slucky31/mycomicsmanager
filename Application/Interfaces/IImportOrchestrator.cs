namespace Application.Interfaces;

public interface IImportOrchestrator
{
    Task ProcessAsync(Guid importJobId, CancellationToken ct = default);
}

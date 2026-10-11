namespace Application.Interfaces;

public interface IIsbnScanOrchestrator
{
    Task ScanLibraryAsync(Guid libraryId, CancellationToken ct = default);
}

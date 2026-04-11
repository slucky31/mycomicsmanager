using Domain.ImportJobs;

namespace Application.Interfaces;

public interface IImportJobRepository
{
    void Add(ImportJob importJob);
    void Remove(ImportJob importJob);
    Task<ImportJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ImportJob>> GetByLibraryIdAsync(Guid libraryId, CancellationToken ct = default);
    Task<IReadOnlyList<ImportJob>> GetPendingAsync(CancellationToken ct = default);
    void Update(ImportJob importJob);
}

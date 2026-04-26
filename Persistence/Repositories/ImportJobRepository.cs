using Application.Interfaces;
using Domain.ImportJobs;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class ImportJobRepository(ApplicationDbContext context) : IImportJobRepository
{
    public void Add(ImportJob importJob) => context.ImportJobs.Add(importJob);

    public void Remove(ImportJob importJob) => context.ImportJobs.Remove(importJob);

    public async Task<ImportJob?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.ImportJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task<IReadOnlyList<ImportJob>> GetByLibraryIdAsync(Guid libraryId, CancellationToken ct = default)
        => await context.ImportJobs
            .AsNoTracking()
            .Where(j => j.LibraryId == libraryId)
            .OrderByDescending(j => j.CreatedAt)
            .ThenBy(j => j.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ImportJob>> GetPendingAsync(CancellationToken ct = default)
        => await context.ImportJobs
            .Where(j => j.Status == ImportJobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .ThenBy(j => j.Id)
            .ToListAsync(ct);

    public async Task<bool> ExistsActiveForFilePathAsync(string filePath, CancellationToken ct = default)
        => await context.ImportJobs
            .AnyAsync(j => j.OriginalFilePath == filePath
                        && j.Status != ImportJobStatus.Completed
                        && j.Status != ImportJobStatus.Failed, ct);

    public void Update(ImportJob importJob) => context.ImportJobs.Update(importJob);
}

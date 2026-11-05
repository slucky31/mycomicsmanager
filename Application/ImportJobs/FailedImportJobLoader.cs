using Application.Interfaces;
using Domain.ImportJobs;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.ImportJobs;

// A failed import job of the user, with its library (shared by the retry and the abandon of a failed import).
public sealed class FailedImportJobLoader(
    IImportJobRepository importJobRepository,
    IRepository<Library, Guid> libraryRepository)
{
    public async Task<Result<(ImportJob Job, Library Library)>> LoadAsync(
        Guid importJobId, Guid userId, CancellationToken cancellationToken)
    {
        if (importJobId == Guid.Empty || userId == Guid.Empty)
        {
            return ImportJobError.BadRequest;
        }

        var job = await importJobRepository.GetByIdAsync(importJobId, cancellationToken);
        if (job is null)
        {
            return ImportJobError.NotFound;
        }

        var library = await libraryRepository.GetByIdAsync(job.LibraryId);
        if (library is null || library.UserId != userId)
        {
            return ImportJobError.NotFound;
        }

        if (job.Status != ImportJobStatus.Failed)
        {
            return ImportJobError.NotFailed;
        }

        return (job, library);
    }
}

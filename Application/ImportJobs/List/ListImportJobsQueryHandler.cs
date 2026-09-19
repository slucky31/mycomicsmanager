using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.ImportJobs;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.ImportJobs.List;

public sealed class ListImportJobsQueryHandler(
    IImportJobRepository importJobRepository,
    IRepository<Library, Guid> libraryRepository)
    : IQueryHandler<ListImportJobsQuery, IReadOnlyList<ImportJob>>
{
    public async Task<Result<IReadOnlyList<ImportJob>>> Handle(ListImportJobsQuery query, CancellationToken cancellationToken)
    {
        if (query.LibraryId == Guid.Empty || query.UserId == Guid.Empty)
        {
            return ImportJobError.BadRequest;
        }

        var library = await libraryRepository.GetByIdAsync(query.LibraryId);
        if (library is null || library.UserId != query.UserId)
        {
            return LibrariesError.NotFound;
        }

        var jobs = await importJobRepository.GetByLibraryIdAsync(query.LibraryId, cancellationToken);
        return Result<IReadOnlyList<ImportJob>>.Success(jobs);
    }
}

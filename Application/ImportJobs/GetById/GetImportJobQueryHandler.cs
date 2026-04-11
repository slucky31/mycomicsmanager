using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.ImportJobs;
using Domain.Primitives;

namespace Application.ImportJobs.GetById;

public sealed class GetImportJobQueryHandler(IImportJobRepository importJobRepository)
    : IQueryHandler<GetImportJobQuery, ImportJob>
{
    public async Task<Result<ImportJob>> Handle(GetImportJobQuery query, CancellationToken cancellationToken)
    {
        if (query.ImportJobId == Guid.Empty)
        {
            return ImportJobError.BadRequest;
        }

        var importJob = await importJobRepository.GetByIdAsync(query.ImportJobId, cancellationToken);
        if (importJob is null)
        {
            return ImportJobError.NotFound;
        }

        return importJob;
    }
}

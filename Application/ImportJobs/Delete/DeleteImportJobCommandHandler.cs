using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.ImportJobs;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.ImportJobs.Delete;

public sealed class DeleteImportJobCommandHandler(
    IImportJobRepository importJobRepository,
    IRepository<Library, Guid> libraryRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteImportJobCommand>
{
    public async Task<Result> Handle(DeleteImportJobCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        var job = await importJobRepository.GetByIdAsync(request.ImportJobId, cancellationToken);
        if (job is null)
        {
            return ImportJobError.NotFound;
        }

        var library = await libraryRepository.GetByIdAsync(job.LibraryId);
        if (library is null || library.UserId != request.UserId)
        {
            return ImportJobError.NotFound;
        }

        if (job.Status is not (ImportJobStatus.Completed or ImportJobStatus.Failed))
        {
            return ImportJobError.NotTerminal;
        }

        importJobRepository.Remove(job);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

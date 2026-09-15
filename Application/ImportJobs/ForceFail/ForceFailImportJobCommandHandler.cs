using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.ImportJobs;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.ImportJobs.ForceFail;

public sealed class ForceFailImportJobCommandHandler(
    IImportJobRepository importJobRepository,
    IRepository<Library, Guid> libraryRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ForceFailImportJobCommand>
{
    public async Task<Result> Handle(ForceFailImportJobCommand request, CancellationToken cancellationToken)
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

        if (job.Status is ImportJobStatus.Completed or ImportJobStatus.Failed)
        {
            return ImportJobError.AlreadyFailed;
        }

        job.Fail(job.Status.ToString(), "Marqué en échec manuellement.");
        importJobRepository.Update(job);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

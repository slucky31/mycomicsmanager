using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Primitives;

namespace Application.ImportJobs.Abandon;

// Deletes a failed import and its file, so that nothing is left in the errors directory.
public sealed class AbandonImportJobCommandHandler(
    FailedImportJobLoader failedJobLoader,
    IImportJobRepository importJobRepository,
    IImportDirectoryStorage importStorage,
    IUnitOfWork unitOfWork) : ICommandHandler<AbandonImportJobCommand>
{
    public async Task<Result> Handle(AbandonImportJobCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        var loaded = await failedJobLoader.LoadAsync(request.ImportJobId, request.UserId, cancellationToken);
        if (loaded.IsFailure)
        {
            return loaded.Error!;
        }

        var (job, library) = loaded.Value;

        // No file left (removed by hand): the job alone is deleted.
        var located = importStorage.LocateFailedFile(library.ImportDirectoryName, job.OriginalFilePath, job.ErrorFilePath);
        if (located.IsSuccess)
        {
            var deleted = importStorage.DeleteOriginalFile(located.Value!);
            if (deleted.IsFailure)
            {
                return deleted.Error!;
            }
        }

        importJobRepository.Remove(job);
        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error! : Result.Success();
    }
}

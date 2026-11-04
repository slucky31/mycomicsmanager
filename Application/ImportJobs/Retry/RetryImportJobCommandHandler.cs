using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.ImportJobs;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Application.ImportJobs.Retry;

public sealed class RetryImportJobCommandHandler(
    FailedImportJobLoader failedJobLoader,
    IImportJobRepository importJobRepository,
    IImportDirectoryStorage importStorage,
    IImportJobEnqueuer enqueuer,
    IUnitOfWork unitOfWork,
    ILogger<RetryImportJobCommandHandler> logger) : ICommandHandler<RetryImportJobCommand>
{
    private const string RetryStep = "Retry";

    public async Task<Result> Handle(RetryImportJobCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        var loaded = await failedJobLoader.LoadAsync(request.ImportJobId, request.UserId, cancellationToken);
        if (loaded.IsFailure)
        {
            return loaded.Error!;
        }

        var (job, library) = loaded.Value;
        var located = importStorage.LocateFailedFile(library.ImportDirectoryName, job.OriginalFilePath, job.ErrorFilePath);
        if (located.IsFailure)
        {
            return ImportJobError.FailedFileNotFound;
        }

        var sourcePath = located.Value!;
        var alreadyInPlace = IsDirectlyInLibraryDirectory(sourcePath, library.ImportDirectoryName);
        var destinationPath = sourcePath;
        if (!alreadyInPlace)
        {
            var available = importStorage.GetAvailableFilePath(library.ImportDirectoryName, Path.GetFileName(job.OriginalFilePath));
            if (available.IsFailure)
            {
                return available.Error!;
            }

            destinationPath = available.Value!;
        }

        // Saved as pending on its new path before the file comes back, so that the watcher sees an active job
        // for that file and does not create a second one.
        var retryResult = job.Retry(destinationPath);
        if (retryResult.IsFailure)
        {
            return retryResult.Error!;
        }

        importJobRepository.Update(job);
        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error!;
        }

        if (!alreadyInPlace)
        {
            var deposit = importStorage.DepositFile(sourcePath, destinationPath);
            if (deposit.IsFailure)
            {
                return await RestoreFailureAsync(job, sourcePath, deposit.Error!, cancellationToken);
            }
        }

        enqueuer.Enqueue(job.Id);
        return Result.Success();
    }

    // The file still sits where the watcher reads it (an import marked as failed by hand): no move needed.
    private static bool IsDirectlyInLibraryDirectory(string sourcePath, string directoryName)
        => string.Equals(Path.GetFileName(Path.GetDirectoryName(sourcePath)), directoryName, StringComparison.Ordinal);

    private async Task<Result> RestoreFailureAsync(ImportJob job, string sourcePath, TError error, CancellationToken ct)
    {
        job.Fail(RetryStep, error.Description ?? error.Code);
        job.RecordErrorFile(sourcePath);
        importJobRepository.Update(job);
        var saveResult = await unitOfWork.SaveChangesAsync(ct);
        if (saveResult.IsFailure)
        {
            logger.LogError("Could not mark import job {JobId} as failed again after a failed retry: {Error}",
                job.Id, saveResult.Error?.Description);
        }

        return error;
    }
}

using Application.Abstractions.Messaging;
using Ardalis.GuardClauses;
using Domain.ImportJobs;
using Domain.Primitives;

namespace Application.ImportJobs.DownloadFile;

public sealed class GetFailedImportFileQueryHandler(
    FailedImportJobLoader failedJobLoader,
    IImportDirectoryStorage importStorage) : IQueryHandler<GetFailedImportFileQuery, FailedImportFile>
{
    public async Task<Result<FailedImportFile>> Handle(GetFailedImportFileQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        var loaded = await failedJobLoader.LoadAsync(request.ImportJobId, request.UserId, cancellationToken);
        if (loaded.IsFailure)
        {
            return loaded.Error!;
        }

        var (job, library) = loaded.Value;

        // Same lookup as a retry: errors directory first, then where the file was imported from.
        var located = importStorage.LocateFailedFile(library.ImportDirectoryName, job.OriginalFilePath, job.ErrorFilePath);
        if (located.IsFailure)
        {
            return ImportJobError.FailedFileNotFound;
        }

        return new FailedImportFile(located.Value!, job.OriginalFileName);
    }
}

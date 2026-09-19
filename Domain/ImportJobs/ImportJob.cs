using Domain.Primitives;

namespace Domain.ImportJobs;

public class ImportJob : Entity<Guid>
{
    private static readonly ImportJobStatus[] s_orderedStatuses =
    [
        ImportJobStatus.Pending,
        ImportJobStatus.Extracting,
        ImportJobStatus.Converting,
        ImportJobStatus.SearchingMetadata,
        ImportJobStatus.UploadingCover,
        ImportJobStatus.BuildingArchive,
        ImportJobStatus.Completed
    ];

    public string OriginalFileName { get; private set; } = string.Empty;

    public string OriginalFilePath { get; private set; } = string.Empty;

    public long OriginalFileSize { get; private set; }

    public Guid LibraryId { get; private set; }

    public Guid? DigitalBookId { get; private set; }

    public ImportJobStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? ErrorStep { get; private set; }

    public int ConvertedImagesCount { get; private set; }

    public int TotalImagesToConvert { get; private set; }

    protected ImportJob() { }

    public static Result<ImportJob> Create(
        string originalFileName,
        string originalFilePath,
        long originalFileSize,
        Guid libraryId)
    {
        if (string.IsNullOrWhiteSpace(originalFileName) ||
            string.IsNullOrWhiteSpace(originalFilePath) ||
            originalFileSize <= 0 ||
            libraryId == Guid.Empty)
        {
            return ImportJobError.BadRequest;
        }

        var job = new ImportJob
        {
            Id = Guid.CreateVersion7(),
            OriginalFileName = originalFileName,
            OriginalFilePath = originalFilePath,
            OriginalFileSize = originalFileSize,
            LibraryId = libraryId,
            Status = ImportJobStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        return job;
    }

    public void UpdateConversionProgress(int converted, int total)
    {
        ConvertedImagesCount = converted;
        TotalImagesToConvert = total;
    }

    public Result Advance(ImportJobStatus newStatus)
    {
        if (Status == ImportJobStatus.Completed)
        {
            return ImportJobError.AlreadyCompleted;
        }

        if (Status == ImportJobStatus.Failed)
        {
            return ImportJobError.AlreadyFailed;
        }

        var currentIndex = Array.IndexOf(s_orderedStatuses, Status);
        var newIndex = Array.IndexOf(s_orderedStatuses, newStatus);

        if (newIndex != currentIndex + 1)
        {
            return ImportJobError.InvalidStatusTransition;
        }

        Status = newStatus;
        return Result.Success();
    }

    public Result Fail(string step, string errorMessage)
    {
        if (Status == ImportJobStatus.Completed)
        {
            return ImportJobError.AlreadyCompleted;
        }

        if (Status == ImportJobStatus.Failed)
        {
            return ImportJobError.AlreadyFailed;
        }

        Status = ImportJobStatus.Failed;
        ErrorStep = step;
        ErrorMessage = errorMessage;
        return Result.Success();
    }

    public Result Complete(Guid digitalBookId)
    {
        if (Status == ImportJobStatus.Failed)
        {
            return ImportJobError.AlreadyFailed;
        }

        if (Status != ImportJobStatus.BuildingArchive)
        {
            return ImportJobError.InvalidStatusTransition;
        }

        Status = ImportJobStatus.Completed;
        DigitalBookId = digitalBookId;
        CompletedAt = DateTime.UtcNow;
        return Result.Success();
    }
}

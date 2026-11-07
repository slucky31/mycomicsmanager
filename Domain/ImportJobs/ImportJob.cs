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

    // Where the original file was moved after the failure (errors directory of the library), used by a retry.
    public string? ErrorFilePath { get; private set; }

    public int ConvertedImagesCount { get; private set; }

    public int TotalImagesToConvert { get; private set; }

    // The archives found inside a split archive, as deposited in the import directory.
    public IReadOnlyList<string> SplitFileNames { get; private set; } = [];

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
        if (TerminalError() is { } terminalError)
        {
            return terminalError;
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
        if (TerminalError() is { } terminalError)
        {
            return terminalError;
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

    // An archive holding other archives instead of pages (several volumes in one download): each one was
    // deposited in the import directory to be imported on its own, and this import ends there.
    public Result Split(IReadOnlyList<string> fileNames)
    {
        if (TerminalError() is { } terminalError)
        {
            return terminalError;
        }

        if (Status != ImportJobStatus.Extracting)
        {
            return ImportJobError.InvalidStatusTransition;
        }

        if (fileNames is null || fileNames.Count == 0 || fileNames.Any(string.IsNullOrWhiteSpace))
        {
            return ImportJobError.BadRequest;
        }

        Status = ImportJobStatus.Split;
        SplitFileNames = [.. fileNames];
        CompletedAt = DateTime.UtcNow;
        return Result.Success();
    }

    // Completed and Split both end an import that succeeded.
    private TError? TerminalError() => Status switch
    {
        ImportJobStatus.Completed or ImportJobStatus.Split => ImportJobError.AlreadyCompleted,
        ImportJobStatus.Failed => ImportJobError.AlreadyFailed,
        _ => null
    };

    public void RecordErrorFile(string errorFilePath)
    {
        ErrorFilePath = errorFilePath;
    }

    // A failed import goes back to the queue, on the file put back at filePath.
    public Result Retry(string filePath)
    {
        if (Status != ImportJobStatus.Failed)
        {
            return ImportJobError.NotFailed;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return ImportJobError.BadRequest;
        }

        Status = ImportJobStatus.Pending;
        OriginalFilePath = filePath;
        CreatedAt = DateTime.UtcNow;
        CompletedAt = null;
        ErrorMessage = null;
        ErrorStep = null;
        ErrorFilePath = null;
        ConvertedImagesCount = 0;
        TotalImagesToConvert = 0;
        return Result.Success();
    }
}

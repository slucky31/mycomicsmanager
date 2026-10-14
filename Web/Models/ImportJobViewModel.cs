using Domain.ImportJobs;

namespace Web.Models;

public record ImportJobViewModel(
    Guid Id,
    string OriginalFileName,
    long OriginalFileSize,
    string Status,
    string StatusDisplay,
    int ProgressPercent,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? ErrorMessage,
    string? ErrorStep,
    int ConvertedImagesCount,
    int TotalImagesToConvert)
{
    public static ImportJobViewModel From(ImportJob job) => new(
        Id: job.Id,
        OriginalFileName: job.OriginalFileName,
        OriginalFileSize: job.OriginalFileSize,
        Status: job.Status.ToString(),
        StatusDisplay: GetStatusDisplay(job.Status),
        ProgressPercent: GetProgressPercent(job.Status, job.ConvertedImagesCount, job.TotalImagesToConvert),
        CreatedAt: job.CreatedAt,
        CompletedAt: job.CompletedAt,
        ErrorMessage: job.ErrorMessage,
        ErrorStep: job.ErrorStep,
        ConvertedImagesCount: job.ConvertedImagesCount,
        TotalImagesToConvert: job.TotalImagesToConvert)
    {
        DigitalBookId = job.DigitalBookId,
    };

    // The steps of an import in progress, from waiting to building the book (Completed and Failed are not steps).
    public const int StepCount = 6;

    // The book created by the import, once it is completed.
    public Guid? DigitalBookId { get; init; }

    public string? BookHref => Status == nameof(ImportJobStatus.Completed) && DigitalBookId is { } bookId
        ? $"/books/{bookId}"
        : null;

    // "Step 3 of 6 · 42%" while the import runs.
    public string? StepDisplay => Enum.TryParse<ImportJobStatus>(Status, out var status) && StepOf(status) is { } step
        ? $"Step {step} of {StepCount} · {ProgressPercent}%"
        : null;

    // The reason of a failure, readable as is.
    public string? FailureDisplay
    {
        get
        {
            if (Status != nameof(ImportJobStatus.Failed))
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(ErrorMessage) ? "The import failed." : ErrorMessage;
        }
    }

    // "Failed while: Extracting pages"; a step that is not a status (e.g. "Init") is shown as is.
    public string? FailedStepDisplay => Status == nameof(ImportJobStatus.Failed) && !string.IsNullOrWhiteSpace(ErrorStep)
        ? $"Failed while: {StepName(ErrorStep)}"
        : null;

    private static string StepName(string step) =>
        Enum.TryParse<ImportJobStatus>(step, out var status) ? GetStatusDisplay(status) : step;

    public bool IsTerminal => Status is "Completed" or "Failed";

    public string? DurationDisplay =>
        CompletedAt.HasValue
            ? FormatDuration(CompletedAt.Value - CreatedAt)
            : null;

    public string? ConversionDetail =>
        Status == "Converting" && TotalImagesToConvert > 0
            ? $"{ConvertedImagesCount}/{TotalImagesToConvert} images converted"
            : null;

    public string FileSizeDisplay => OriginalFileSize switch
    {
        >= 1_073_741_824 => $"{OriginalFileSize / 1_073_741_824.0:F1} GB",
        >= 1_048_576 => $"{OriginalFileSize / 1_048_576.0:F1} MB",
        >= 1_024 => $"{OriginalFileSize / 1_024.0:F0} KB",
        _ => $"{OriginalFileSize} B"
    };

    public string StatusCssClass => Status switch
    {
        "Pending" => "status-pending",
        "Completed" => "status-completed",
        "Failed" => "status-failed",
        _ => "status-processing"
    };

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 60)
        {
            return $"{(int)duration.TotalSeconds}s";
        }
        if (duration.TotalMinutes < 60)
        {
            return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
        }
        return $"{(int)duration.TotalHours}h {duration.Minutes}m";
    }

    private static string GetStatusDisplay(ImportJobStatus status) => status switch
    {
        ImportJobStatus.Pending => "Waiting",
        ImportJobStatus.Extracting => "Extracting pages",
        ImportJobStatus.Converting => "Converting pages",
        ImportJobStatus.SearchingMetadata => "Searching details",
        ImportJobStatus.UploadingCover => "Uploading cover",
        ImportJobStatus.BuildingArchive => "Building the book",
        ImportJobStatus.Completed => "Imported",
        ImportJobStatus.Failed => "Failed",
        _ => status.ToString()
    };

    private static int? StepOf(ImportJobStatus status) => status switch
    {
        ImportJobStatus.Pending => 1,
        ImportJobStatus.Extracting => 2,
        ImportJobStatus.Converting => 3,
        ImportJobStatus.SearchingMetadata => 4,
        ImportJobStatus.UploadingCover => 5,
        ImportJobStatus.BuildingArchive => 6,
        _ => null
    };

    private static int GetProgressPercent(ImportJobStatus status, int converted, int total) => status switch
    {
        ImportJobStatus.Pending => 0,
        ImportJobStatus.Extracting => 15,
        ImportJobStatus.Converting => total > 0
            ? 35 + converted * 20 / total
            : 35,
        ImportJobStatus.SearchingMetadata => 55,
        ImportJobStatus.UploadingCover => 70,
        ImportJobStatus.BuildingArchive => 85,
        ImportJobStatus.Completed => 100,
        ImportJobStatus.Failed => 0,
        _ => 0
    };
}

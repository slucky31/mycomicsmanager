using Domain.ImportJobs;
using MudBlazor;

namespace Web.Models;

public record ImportJobViewModel(
    Guid Id,
    string OriginalFileName,
    long OriginalFileSize,
    string Status,
    string StatusDisplay,
    Color StatusColor,
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
        StatusColor: GetStatusColor(job.Status),
        ProgressPercent: GetProgressPercent(job.Status, job.ConvertedImagesCount, job.TotalImagesToConvert),
        CreatedAt: job.CreatedAt,
        CompletedAt: job.CompletedAt,
        ErrorMessage: job.ErrorMessage,
        ErrorStep: job.ErrorStep,
        ConvertedImagesCount: job.ConvertedImagesCount,
        TotalImagesToConvert: job.TotalImagesToConvert);

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
        ImportJobStatus.Pending => "Pending",
        ImportJobStatus.Extracting => "Extracting...",
        ImportJobStatus.Converting => "Converting images...",
        ImportJobStatus.SearchingMetadata => "Searching metadata...",
        ImportJobStatus.UploadingCover => "Uploading cover...",
        ImportJobStatus.BuildingArchive => "Building archive...",
        ImportJobStatus.Completed => "Completed",
        ImportJobStatus.Failed => "Failed",
        _ => status.ToString()
    };

    private static Color GetStatusColor(ImportJobStatus status) => status switch
    {
        ImportJobStatus.Pending => Color.Default,
        ImportJobStatus.Extracting => Color.Info,
        ImportJobStatus.Converting => Color.Info,
        ImportJobStatus.SearchingMetadata => Color.Info,
        ImportJobStatus.UploadingCover => Color.Info,
        ImportJobStatus.BuildingArchive => Color.Info,
        ImportJobStatus.Completed => Color.Success,
        ImportJobStatus.Failed => Color.Error,
        _ => Color.Default
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

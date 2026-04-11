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
    string? ErrorStep)
{
    public static ImportJobViewModel From(ImportJob job) => new(
        Id: job.Id,
        OriginalFileName: job.OriginalFileName,
        OriginalFileSize: job.OriginalFileSize,
        Status: job.Status.ToString(),
        StatusDisplay: GetStatusDisplay(job.Status),
        StatusColor: GetStatusColor(job.Status),
        ProgressPercent: GetProgressPercent(job.Status),
        CreatedAt: job.CreatedAt,
        CompletedAt: job.CompletedAt,
        ErrorMessage: job.ErrorMessage,
        ErrorStep: job.ErrorStep);

    public bool IsTerminal => Status is "Completed" or "Failed";

    public string FileSizeDisplay => OriginalFileSize switch
    {
        >= 1_073_741_824 => $"{OriginalFileSize / 1_073_741_824.0:F1} Go",
        >= 1_048_576 => $"{OriginalFileSize / 1_048_576.0:F1} Mo",
        >= 1_024 => $"{OriginalFileSize / 1_024.0:F0} Ko",
        _ => $"{OriginalFileSize} o"
    };

    private static string GetStatusDisplay(ImportJobStatus status) => status switch
    {
        ImportJobStatus.Pending => "En attente",
        ImportJobStatus.Extracting => "Extraction...",
        ImportJobStatus.Converting => "Conversion images...",
        ImportJobStatus.SearchingMetadata => "Recherche métadonnées...",
        ImportJobStatus.UploadingCover => "Upload couverture...",
        ImportJobStatus.BuildingArchive => "Construction archive...",
        ImportJobStatus.Completed => "Terminé",
        ImportJobStatus.Failed => "Échoué",
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

    private static int GetProgressPercent(ImportJobStatus status) => status switch
    {
        ImportJobStatus.Pending => 0,
        ImportJobStatus.Extracting => 15,
        ImportJobStatus.Converting => 35,
        ImportJobStatus.SearchingMetadata => 55,
        ImportJobStatus.UploadingCover => 70,
        ImportJobStatus.BuildingArchive => 85,
        ImportJobStatus.Completed => 100,
        ImportJobStatus.Failed => 0,
        _ => 0
    };
}

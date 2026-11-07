namespace Domain.ImportJobs;

public static class ImportJobStatusExtensions
{
    // An import that no longer runs: it succeeded, failed, or was split into other imports.
    public static bool IsTerminal(this ImportJobStatus status) =>
        status is ImportJobStatus.Completed or ImportJobStatus.Failed or ImportJobStatus.Split;

    // A split archive counts as a success: its file is gone and each archive found inside is imported on its own.
    public static bool IsSucceeded(this ImportJobStatus status) =>
        status is ImportJobStatus.Completed or ImportJobStatus.Split;
}

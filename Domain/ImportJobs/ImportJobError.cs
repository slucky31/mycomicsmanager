using Domain.Primitives;

namespace Domain.ImportJobs;

public static class ImportJobError
{
    public static readonly TError BadRequest = new("IMP400", "Verify the import request parameters.");
    public static readonly TError NotFound = new("IMP404", "Import job not found.");
    public static readonly TError InvalidStatusTransition = new("IMP405", "Invalid status transition.");
    public static readonly TError AlreadyCompleted = new("IMP409", "Import job is already completed.");
    public static readonly TError AlreadyFailed = new("IMP410", "Import job has already failed.");
    public static readonly TError NotTerminal = new("IMP412", "Only completed or failed import jobs can be deleted.");
    public static readonly TError UnhandledException = new("IMP500", "An unexpected error occurred during import.");
    public static readonly TError InsufficientDiskSpace = new("IMP507", "Insufficient disk space to process the import.");
}

using Application.Abstractions.Messaging;

namespace Application.ImportJobs.DownloadFile;

// The file of a failed import, downloaded to analyse it without connecting to the server (#1291).
public record GetFailedImportFileQuery(Guid ImportJobId, Guid UserId) : IQuery<FailedImportFile>;

// FilePath: where the file is on the server; FileName: the name it was imported with.
public sealed record FailedImportFile(string FilePath, string FileName);

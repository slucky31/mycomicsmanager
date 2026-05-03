using Application.Abstractions.Messaging;
using Domain.ImportJobs;

namespace Application.ImportJobs.Create;

public record CreateImportJobCommand(
    string OriginalFileName,
    string OriginalFilePath,
    long OriginalFileSize,
    Guid LibraryId,
    Guid UserId
) : ICommand<ImportJob>;

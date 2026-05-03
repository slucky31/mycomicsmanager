using Application.Abstractions.Messaging;

namespace Application.ImportJobs.Delete;

public record DeleteImportJobCommand(Guid ImportJobId, Guid UserId) : ICommand;

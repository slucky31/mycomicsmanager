using Application.Abstractions.Messaging;

namespace Application.ImportJobs.Retry;

public record RetryImportJobCommand(Guid ImportJobId, Guid UserId) : ICommand;

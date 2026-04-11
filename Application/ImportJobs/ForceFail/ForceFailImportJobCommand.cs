using Application.Abstractions.Messaging;

namespace Application.ImportJobs.ForceFail;

public record ForceFailImportJobCommand(Guid ImportJobId, Guid UserId) : ICommand;

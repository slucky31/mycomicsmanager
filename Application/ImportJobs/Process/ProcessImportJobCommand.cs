using Application.Abstractions.Messaging;

namespace Application.ImportJobs.Process;

public record ProcessImportJobCommand(Guid ImportJobId) : ICommand<ImportJobOutcome>;

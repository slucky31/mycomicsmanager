using Application.Abstractions.Messaging;

namespace Application.ImportJobs.Abandon;

public record AbandonImportJobCommand(Guid ImportJobId, Guid UserId) : ICommand;

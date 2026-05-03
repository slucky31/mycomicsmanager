using Application.Abstractions.Messaging;
using Domain.Books;

namespace Application.ImportJobs.Process;

public record ProcessImportJobCommand(Guid ImportJobId) : ICommand<DigitalBook>;

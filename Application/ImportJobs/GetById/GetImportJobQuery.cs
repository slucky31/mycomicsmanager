using Application.Abstractions.Messaging;
using Domain.ImportJobs;

namespace Application.ImportJobs.GetById;

public record GetImportJobQuery(Guid ImportJobId) : IQuery<ImportJob>;

using Application.Abstractions.Messaging;
using Domain.ImportJobs;

namespace Application.ImportJobs.List;

public record ListImportJobsQuery(Guid LibraryId, Guid UserId) : IQuery<IReadOnlyList<ImportJob>>;

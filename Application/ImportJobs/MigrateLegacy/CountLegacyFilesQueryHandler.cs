using Application.Abstractions.Messaging;
using Ardalis.GuardClauses;
using Domain.Primitives;

namespace Application.ImportJobs.MigrateLegacy;

public sealed class CountLegacyFilesQueryHandler(ILegacyLibrarySource legacyLibrarySource) : IQueryHandler<CountLegacyFilesQuery, int>
{
    public Task<Result<int>> Handle(CountLegacyFilesQuery query, CancellationToken cancellationToken)
    {
        Guard.Against.Null(query);
        return Task.FromResult(legacyLibrarySource.CountFiles());
    }
}

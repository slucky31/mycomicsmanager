using Domain.FeedImports;

namespace Application.Interfaces;

public interface IFeedImportDecisionReadService
{
    Task<IPagedList<FeedImportDecision>> GetPagedAsync(
        Guid userId,
        FeedImportDecisionStatus? status,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    // Entries split into several books (at least one sibling decision exists).
    Task<IReadOnlySet<long>> GetMultiBookEntryIdsAsync(
        Guid userId,
        IReadOnlyCollection<long> minifluxEntryIds,
        CancellationToken cancellationToken = default);

    Task<int> CountByStatusAsync(Guid userId, FeedImportDecisionStatus status, CancellationToken cancellationToken = default);
}

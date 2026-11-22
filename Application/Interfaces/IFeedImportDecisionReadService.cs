using Application.FeedImports.List;
using Domain.FeedImports;

namespace Application.Interfaces;

public interface IFeedImportDecisionReadService
{
    Task<IPagedList<FeedImportDecision>> GetPagedAsync(
        Guid userId,
        FeedImportDecisionStatus? status,
        string? host,
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

    // Feeds downloaded since the date after at least one automatic retry (#1318), even when their decision
    // was deleted from the Feeds page since.
    Task<int> CountDownloadedAfterAutoRetryAsync(Guid userId, DateTime since, CancellationToken cancellationToken = default);

    // The number of decisions of the user in each status, on a host when one is given; a status without decision is left out.
    Task<IReadOnlyDictionary<FeedImportDecisionStatus, int>> CountPerStatusAsync(Guid userId, string? host, CancellationToken cancellationToken = default);

    // The number of decisions of the user on each host, in a status when one is given.
    Task<FeedImportHostCounts> CountPerHostAsync(Guid userId, FeedImportDecisionStatus? status, CancellationToken cancellationToken = default);
}

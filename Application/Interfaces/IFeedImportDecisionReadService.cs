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
}

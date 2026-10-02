using Domain.FeedImports;

namespace Application.Interfaces;

public interface IFeedImportDecisionRepository
{
    void Add(FeedImportDecision decision);
    void Remove(FeedImportDecision decision);
    Task<FeedImportDecision?> GetByMinifluxEntryIdAsync(Guid userId, long minifluxEntryId, CancellationToken ct = default);
}

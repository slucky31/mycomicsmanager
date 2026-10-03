using Domain.FeedImports;

namespace Application.Interfaces;

public interface IFeedImportDecisionRepository
{
    void Add(FeedImportDecision decision);
    void Remove(FeedImportDecision decision);
    Task<FeedImportDecision?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FeedImportDecision?> GetByMinifluxEntryIdAsync(Guid userId, long minifluxEntryId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetPendingIdsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetIdsByStatusAsync(Guid userId, FeedImportDecisionStatus status, CancellationToken ct = default);
}

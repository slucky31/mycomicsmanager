using Application.Interfaces;
using Domain.FeedImports;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class FeedImportDecisionRepository(ApplicationDbContext context) : IFeedImportDecisionRepository
{
    public void Add(FeedImportDecision decision) => context.FeedImportDecisions.Add(decision);

    public void Remove(FeedImportDecision decision) => context.FeedImportDecisions.Remove(decision);

    public async Task<FeedImportDecision?> GetByMinifluxEntryIdAsync(Guid userId, long minifluxEntryId, CancellationToken ct = default)
        => await context.FeedImportDecisions
            .FirstOrDefaultAsync(d => d.UserId == userId && d.MinifluxEntryId == minifluxEntryId, ct);
}

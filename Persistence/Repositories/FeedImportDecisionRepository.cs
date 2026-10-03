using Application.Interfaces;
using Domain.FeedImports;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class FeedImportDecisionRepository(ApplicationDbContext context) : IFeedImportDecisionRepository
{
    public void Add(FeedImportDecision decision) => context.FeedImportDecisions.Add(decision);

    public void Remove(FeedImportDecision decision) => context.FeedImportDecisions.Remove(decision);

    public async Task<FeedImportDecision?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.FeedImportDecisions.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<FeedImportDecision?> GetByMinifluxEntryIdAsync(Guid userId, long minifluxEntryId, CancellationToken ct = default)
        => await context.FeedImportDecisions
            .FirstOrDefaultAsync(d => d.UserId == userId && d.MinifluxEntryId == minifluxEntryId && d.ItemIndex == 0, ct);

    public async Task<IReadOnlyList<Guid>> GetPendingIdsAsync(Guid userId, CancellationToken ct = default)
        => await context.FeedImportDecisions
            .AsNoTracking()
            .Where(d => d.UserId == userId && d.Status == FeedImportDecisionStatus.Pending && d.ItemIndex == 0)
            .OrderBy(d => d.CreatedAt)
            .ThenBy(d => d.Id)
            .Select(d => d.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetIdsByStatusAsync(Guid userId, FeedImportDecisionStatus status, CancellationToken ct = default)
        => await context.FeedImportDecisions
            .AsNoTracking()
            .Where(d => d.UserId == userId && d.Status == status)
            .OrderBy(d => d.CreatedAt)
            .ThenBy(d => d.MinifluxEntryId)
            .ThenBy(d => d.ItemIndex)
            .ThenBy(d => d.Id)
            .Select(d => d.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FeedImportDecision>> GetByStatusAsync(Guid userId, FeedImportDecisionStatus status, CancellationToken ct = default)
        => await context.FeedImportDecisions
            .Where(d => d.UserId == userId && d.Status == status)
            .OrderBy(d => d.CreatedAt)
            .ThenBy(d => d.Id)
            .ToListAsync(ct);
}

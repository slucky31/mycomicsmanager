using Application.Interfaces;
using Domain.FeedImports;
using Microsoft.EntityFrameworkCore;
using Persistence.Queries.Helpers;

namespace Persistence.Queries;

public class FeedImportDecisionReadService(ApplicationDbContext context) : IFeedImportDecisionReadService
{
    public async Task<IPagedList<FeedImportDecision>> GetPagedAsync(
        Guid userId,
        FeedImportDecisionStatus? status,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.FeedImportDecisions
            .AsNoTracking()
            .Include(d => d.Events.OrderBy(e => e.OccurredAt).ThenBy(e => e.Id))
            .AsSplitQuery()
            .Where(d => d.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{LikePatternHelper.EscapeLikeSpecialChars(searchTerm)}%";
            query = query.Where(d =>
                EF.Functions.ILike(d.EntryTitle, pattern, @"\") ||
                EF.Functions.ILike(d.Reason, pattern, @"\") ||
                (d.ParsedSerie != null && EF.Functions.ILike(d.ParsedSerie, pattern, @"\")));
        }

        query = query
            .OrderByDescending(d => d.CreatedAt)
            .ThenBy(d => d.Id);

        return await new PagedList<FeedImportDecision>(query).ExecuteQueryAsync(page, pageSize, cancellationToken);
    }
}

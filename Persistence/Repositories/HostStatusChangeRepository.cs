using Application.Interfaces;
using Domain.FeedImports;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class HostStatusChangeRepository(ApplicationDbContext context) : IHostStatusChangeRepository
{
    public void Add(HostStatusChange change) => context.HostStatusChanges.Add(change);

    public async Task<IReadOnlyDictionary<string, HostStatusChange>> GetLatestPerHostAsync(CancellationToken ct = default)
    {
        var latest = await context.HostStatusChanges
            .AsNoTracking()
            .Where(c => !context.HostStatusChanges.Any(n => n.Host == c.Host && (n.ChangedAt > c.ChangedAt || (n.ChangedAt == c.ChangedAt && n.Id.CompareTo(c.Id) > 0))))
            .ToListAsync(ct);
        return latest.ToDictionary(c => c.Host, StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<HostStatusChange>> ListSinceAsync(DateTime since, CancellationToken ct = default)
        => await context.HostStatusChanges
            .AsNoTracking()
            .Where(c => c.ChangedAt >= since)
            .OrderByDescending(c => c.ChangedAt)
            .ThenByDescending(c => c.Id)
            .ToListAsync(ct);

    public Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default)
        => context.HostStatusChanges
            .Where(c => c.ChangedAt < cutoff && context.HostStatusChanges.Any(n => n.Host == c.Host && n.ChangedAt > c.ChangedAt))
            .ExecuteDeleteAsync(ct);
}

using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.FeedImports;
using Domain.Primitives;

namespace Application.FeedImports.HostStatus;

public sealed class GetHostStatusOverviewQueryHandler(
    IDebridLinkClient debridLinkClient,
    IHostStatusChangeRepository changeRepository,
    TimeProvider timeProvider) : IQueryHandler<GetHostStatusOverviewQuery, HostStatusOverview>
{
    private static readonly TimeSpan s_countWindow = TimeSpan.FromDays(30);

    public async Task<Result<HostStatusOverview>> Handle(GetHostStatusOverviewQuery query, CancellationToken cancellationToken)
    {
        Guard.Against.Null(query);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var changes = await changeRepository.ListSinceAsync(utcNow - FeedImportConstants.HostStatusRetention, cancellationToken);
        var latest = await changeRepository.GetLatestPerHostAsync(cancellationToken);
        var hosts = await debridLinkClient.GetHostsAsync(cancellationToken);

        var history = new HostHistory(changes, utcNow - s_countWindow);
        IEnumerable<HostStatusRow> rows = hosts.IsSuccess
            ? hosts.Value!.Select(h => history.ToRow(h.Name, h.IsUp, h.Domains, latest))
            : latest.Values.Select(c => history.ToRow(c.Host, c.IsUp, [], latest));

        // Down first, then by name.
        var sorted = rows.OrderBy(r => r.IsUp).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();
        return new HostStatusOverview(sorted, changes, hosts.IsSuccess);
    }

    // changes: newest first. The first reading of a host is not a change of state.
    private sealed class HostHistory(IReadOnlyList<HostStatusChange> changes, DateTime countFrom)
    {
        public HostStatusRow ToRow(string name, bool isUp, IReadOnlyList<string> domains, IReadOnlyDictionary<string, HostStatusChange> latest)
        {
            var host = name.Trim();
            var known = latest.GetValueOrDefault(host);
            // A state different from the latest recorded one is not recorded yet: its date is unknown.
            var since = known is not null && known.IsUp == isUp ? known.ChangedAt : (DateTime?)null;
            var ofHost = changes.Where(c => c.Host == host).ToList();
            var transitions = ofHost.Count(c => c.ChangedAt >= countFrom && ofHost.Exists(p => p.ChangedAt < c.ChangedAt));
            var isFirstReading = since is not null && ofHost.Exists(c => c.ChangedAt == since) && !ofHost.Exists(p => p.ChangedAt < since);
            return new HostStatusRow(host, isUp, domains, since, isFirstReading, transitions);
        }
    }
}

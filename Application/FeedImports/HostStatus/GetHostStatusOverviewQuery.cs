using Application.Abstractions.Messaging;
using Domain.FeedImports;

namespace Application.FeedImports.HostStatus;

// The hosts of the Debrid-Link status page with since when they are in their state, and the recorded changes.
public record GetHostStatusOverviewQuery : IQuery<HostStatusOverview>;

// IsLive: the states come from Debrid-Link now; false when it could not be reached (the latest recorded states are shown).
public sealed record HostStatusOverview(IReadOnlyList<HostStatusRow> Hosts, IReadOnlyList<HostStatusChange> Changes, bool IsLive)
{
    public static HostStatusOverview Empty { get; } = new([], [], false);

    // The row of the host serving this domain (a host of a decision, e.g. "ddl.to"), or null when Debrid-Link does not list it.
    public HostStatusRow? FindByDomain(string domain) =>
        Hosts.FirstOrDefault(h => Domain.Extensions.HostExtension.IsSameOrSubdomainOf(domain, h.Domains));
}

// Since: date of the latest recorded change (null when not recorded yet); IsFirstReading: that date is only when MCM
// first read the host, the state may be older. ChangesLast30Days: times it went up or down.
public sealed record HostStatusRow(string Name, bool IsUp, IReadOnlyList<string> Domains, DateTime? Since, bool IsFirstReading, int ChangesLast30Days);

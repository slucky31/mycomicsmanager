using Domain.Extensions;

namespace Application.FeedImports;

// A host as listed by Debrid-Link (GET /downloader/hosts): the state shown on its status page.
public sealed record DebridLinkHost(string Name, bool IsUp, IReadOnlyList<string> Domains);

// The state of the hosts on Debrid-Link; empty when it could not be read, so nothing is reported down.
public sealed class DebridLinkHostStatuses(IReadOnlyList<DebridLinkHost> hosts)
{
    public static DebridLinkHostStatuses Unknown { get; } = new([]);

    // null: Debrid-Link does not list this host, or its state could not be read.
    public bool? IsUp(string host) => hosts.FirstOrDefault(h => host.IsSameOrSubdomainOf(h.Domains))?.IsUp;

    public bool IsKnownUp(string host) => IsUp(host) == true;

    public bool IsKnownDown(string host) => IsUp(host) == false;
}

public static class DebridLinkClientExtensions
{
    public static async Task<DebridLinkHostStatuses> GetHostStatusesAsync(this IDebridLinkClient client, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        var hosts = await client.GetHostsAsync(cancellationToken);
        return hosts.IsSuccess ? new DebridLinkHostStatuses(hosts.Value!) : DebridLinkHostStatuses.Unknown;
    }
}

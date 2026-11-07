using Application.FeedImports.List;
using Domain.FeedImports;

namespace Web.Models;

// The status filters of the Feeds page, shown as chips (S2).
public enum FeedImportFilter
{
    All,
    ToDecide,
    Failed,
    Downloaded
}

public sealed record FeedImportFilterChip(FeedImportFilter Filter, string Label);

public sealed record FeedImportStatusCounts(int All, int ToDecide, int Failed, int Downloaded)
{
    public static FeedImportStatusCounts Empty { get; } = new(0, 0, 0, 0);

    public static FeedImportStatusCounts From(IReadOnlyDictionary<FeedImportDecisionStatus, int> counts)
    {
        ArgumentNullException.ThrowIfNull(counts);

        return new FeedImportStatusCounts(
            counts.Values.Sum(),
            counts.GetValueOrDefault(FeedImportDecisionStatus.AwaitingArbitration),
            counts.GetValueOrDefault(FeedImportDecisionStatus.Failed),
            counts.GetValueOrDefault(FeedImportDecisionStatus.Downloaded));
    }

    // "All · 15", "To decide · 1"...; a filter with nothing in it shows its name only.
    public IReadOnlyList<FeedImportFilterChip> Chips =>
    [
        Chip(FeedImportFilter.All, All),
        Chip(FeedImportFilter.ToDecide, ToDecide),
        Chip(FeedImportFilter.Failed, Failed),
        Chip(FeedImportFilter.Downloaded, Downloaded),
    ];

    private static FeedImportFilterChip Chip(FeedImportFilter filter, int count) =>
        new(filter, count > 0 ? $"{FeedImportFilters.Name(filter)} · {count}" : FeedImportFilters.Name(filter));
}

// One line of the host filter: a host, the decisions without link (FeedImportConstants.NoHost) or all of them (null).
public sealed record FeedImportHostOption(string? Host, string Label);

public sealed record FeedImportHostOptions(IReadOnlyList<FeedImportHostOption> Options)
{
    public static FeedImportHostOptions Empty { get; } = new([new FeedImportHostOption(null, FeedImportFilters.HostName(null))]);

    // "All hosts · 38", then the hosts, most decisions first, then "No link · 4" when some have none.
    public static FeedImportHostOptions From(FeedImportHostCounts counts)
    {
        ArgumentNullException.ThrowIfNull(counts);

        List<FeedImportHostOption> options = [Option(null, counts.All)];
        options.AddRange(counts.Hosts.Select(h => Option(h.Host, h.Count)));
        if (counts.NoHost > 0)
        {
            options.Add(Option(FeedImportConstants.NoHost, counts.NoHost));
        }

        return new FeedImportHostOptions(options);
    }

    private static FeedImportHostOption Option(string? host, int count) =>
        new(host, count > 0 ? $"{FeedImportFilters.HostName(host)} · {count}" : FeedImportFilters.HostName(host));
}

public static class FeedImportFilters
{
    public static string HostName(string? host) => host switch
    {
        null => "All hosts",
        FeedImportConstants.NoHost => "No link",
        _ => host,
    };

    // The button of the host filter.
    public static string HostButtonLabel(string? host) => host is null ? "Host: All" : $"Host: {HostName(host)}";

    public static string Name(FeedImportFilter filter) => filter switch
    {
        FeedImportFilter.ToDecide => "To decide",
        FeedImportFilter.Failed => "Failed",
        FeedImportFilter.Downloaded => "Downloaded",
        _ => "All",
    };

    public static FeedImportDecisionStatus? StatusOf(FeedImportFilter filter) => filter switch
    {
        FeedImportFilter.ToDecide => FeedImportDecisionStatus.AwaitingArbitration,
        FeedImportFilter.Failed => FeedImportDecisionStatus.Failed,
        FeedImportFilter.Downloaded => FeedImportDecisionStatus.Downloaded,
        _ => null,
    };
}

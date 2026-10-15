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

public static class FeedImportFilters
{
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

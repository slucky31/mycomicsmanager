namespace Application.FeedImports.List;

public sealed record FeedImportHostCount(string Host, int Count);

// All the decisions, those of each host (most decisions first: a decision counts once on each of its hosts),
// and those without any link.
public sealed record FeedImportHostCounts(int All, IReadOnlyList<FeedImportHostCount> Hosts, int NoHost);

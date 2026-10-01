namespace Application.FeedImports;

#pragma warning disable CA1054, CA1056 // Raw URL from Miniflux, validated when the decision is created
public sealed record MinifluxEntry(long Id, string Title, string Url, DateTimeOffset? PublishedAt);
#pragma warning restore CA1054, CA1056

namespace Domain.FeedImports;

#pragma warning disable CA1054, CA1056 // URL serialized as JSON in FeedImportDecision.Links
public sealed record DownloadMirror(string Url, string Host);
#pragma warning restore CA1054, CA1056

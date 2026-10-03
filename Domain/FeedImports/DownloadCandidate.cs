namespace Domain.FeedImports;

// One book (one file) and the links that serve it: several mirrors are the same file on different hosts.
public sealed record DownloadCandidate(string Label, string? FileName, long? SizeBytes, IReadOnlyList<DownloadMirror> Mirrors);

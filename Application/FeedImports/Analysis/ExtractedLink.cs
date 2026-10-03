namespace Application.FeedImports.Analysis;

#pragma warning disable CA1054, CA1056 // Raw link found in an article page
// GroupKey lets a site-specific extractor say which links belong to the same book (e.g. the "Tome 3" block).
public sealed record ExtractedLink(string Url, string Host, string Text, string? FileName, long? SizeBytes, string? GroupKey = null);
#pragma warning restore CA1054, CA1056

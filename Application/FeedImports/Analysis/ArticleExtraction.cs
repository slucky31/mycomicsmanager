namespace Application.FeedImports.Analysis;

// Links found in an article page, plus the book ISBN when the page states it.
// Links: towards FeedImport:AllowedDownloadHosts. UnsupportedLinks: files offered on other hosts, reported so the user
// can download them by hand.
public sealed record ArticleExtraction(IReadOnlyList<ExtractedLink> Links, IReadOnlyList<ExtractedLink> UnsupportedLinks, string? Isbn = null)
{
    public static ArticleExtraction Empty { get; } = new([], []);
}

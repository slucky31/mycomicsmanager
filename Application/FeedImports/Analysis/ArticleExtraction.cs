namespace Application.FeedImports.Analysis;

// Links found in an article page, plus the book ISBN when the page states it.
public sealed record ArticleExtraction(IReadOnlyList<ExtractedLink> Links, string? Isbn = null)
{
    public static ArticleExtraction Empty { get; } = new([]);
}

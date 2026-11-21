using Application.FeedImports.Analysis;

namespace Application.FeedImports.Analyze;

// Where the analysis reads an article: the page itself, then the extractor of its site.
public sealed record ArticleLinkSources(
    IArticlePageFetcher PageFetcher,
    IEnumerable<IDownloadLinkExtractor> Extractors);

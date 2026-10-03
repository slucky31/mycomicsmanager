namespace Application.FeedImports.Analysis;

public interface IDownloadLinkExtractor
{
    // The generic extractor is the fallback, used when no site-specific extractor handles the page.
    bool IsFallback { get; }

    bool CanHandle(Uri pageUri);

    // Only links towards allowedDownloadHosts (subdomains included) are returned. Parsing never executes page content.
    ArticleExtraction Extract(string html, Uri pageUri, IReadOnlyList<string> allowedDownloadHosts);
}

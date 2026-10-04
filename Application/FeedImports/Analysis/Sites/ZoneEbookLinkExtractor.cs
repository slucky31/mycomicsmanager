using Microsoft.Extensions.Logging;

namespace Application.FeedImports.Analysis.Sites;

public sealed class ZoneEbookLinkExtractor(ILogger<ZoneEbookLinkExtractor> logger) : DleArticleLinkExtractor(logger)
{
    protected override string SiteDomain => "zone-ebook.com";

    protected override string ArticleXPath { get; } = ClassXPath("div", "maincont");
}

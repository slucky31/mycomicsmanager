namespace Application.FeedImports.Analysis.Sites;

public sealed class ZoneEbookLinkExtractor : DleArticleLinkExtractor
{
    protected override string SiteDomain => "zone-ebook.com";

    protected override string ArticleXPath { get; } = ClassXPath("div", "maincont");
}

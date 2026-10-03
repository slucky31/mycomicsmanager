namespace Application.FeedImports.Analysis.Sites;

public sealed class PlaneteBdLinkExtractor : DleArticleLinkExtractor
{
    protected override string SiteDomain => "planete-bd.org";

    protected override string ArticleXPath { get; } = ClassXPath("div", "post-content");
}

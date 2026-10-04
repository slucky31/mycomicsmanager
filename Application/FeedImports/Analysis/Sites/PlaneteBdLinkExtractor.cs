using Microsoft.Extensions.Logging;

namespace Application.FeedImports.Analysis.Sites;

public sealed class PlaneteBdLinkExtractor(ILogger<PlaneteBdLinkExtractor> logger) : DleArticleLinkExtractor(logger)
{
    protected override string SiteDomain => "planete-bd.org";

    protected override string ArticleXPath { get; } = ClassXPath("div", "post-content");
}

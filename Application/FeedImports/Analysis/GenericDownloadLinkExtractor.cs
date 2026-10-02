using Domain.Extensions;
using HtmlAgilityPack;

namespace Application.FeedImports.Analysis;

// Fallback: every <a href> pointing to an allowed hoster, in page order.
public sealed class GenericDownloadLinkExtractor : IDownloadLinkExtractor
{
    public bool IsFallback => true;

    public bool CanHandle(Uri pageUri) => true;

    public IReadOnlyList<ExtractedLink> Extract(string html, Uri pageUri, IReadOnlyList<string> allowedDownloadHosts)
    {
        ArgumentNullException.ThrowIfNull(pageUri);
        ArgumentNullException.ThrowIfNull(allowedDownloadHosts);

        if (string.IsNullOrWhiteSpace(html) || allowedDownloadHosts.Count == 0)
        {
            return [];
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);
        var anchors = document.DocumentNode.SelectNodes("//a[@href]");
        if (anchors is null)
        {
            return [];
        }

        var links = new List<ExtractedLink>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var anchor in anchors)
        {
            var link = ToLink(anchor, pageUri, allowedDownloadHosts);
            if (link is not null && seen.Add(link.Url))
            {
                links.Add(link);
            }
        }

        return links;
    }

    internal static ExtractedLink? ToLink(HtmlNode anchor, Uri pageUri, IReadOnlyList<string> allowedDownloadHosts, string? groupKey = null)
    {
        var href = HtmlEntity.DeEntitize(anchor.GetAttributeValue("href", string.Empty)).Trim();
        if (href.Length == 0 ||
            !Uri.TryCreate(pageUri, href, out var url) ||
            (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp) ||
            !url.Host.IsSameOrSubdomainOf(allowedDownloadHosts))
        {
            return null;
        }

        var text = HtmlEntity.DeEntitize(anchor.InnerText).Trim();
        var surroundingText = HtmlEntity.DeEntitize(anchor.ParentNode?.InnerText ?? string.Empty);
        return new ExtractedLink(
            url.AbsoluteUri,
            url.Host,
            text,
            LinkMetadataParser.FindFileName(text, url),
            LinkMetadataParser.FindSizeBytes(text) ?? LinkMetadataParser.FindSizeBytes(surroundingText),
            groupKey);
    }
}

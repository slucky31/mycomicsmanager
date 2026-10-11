using Domain.Extensions;
using HtmlAgilityPack;

namespace Application.FeedImports.Analysis;

// Fallback: every <a href> pointing to an allowed hoster, in page order.
public sealed class GenericDownloadLinkExtractor : IDownloadLinkExtractor
{
    public bool IsFallback => true;

    public bool CanHandle(Uri pageUri) => true;

    public ArticleExtraction Extract(string html, Uri pageUri, IReadOnlyList<string> allowedDownloadHosts)
    {
        ArgumentNullException.ThrowIfNull(pageUri);
        ArgumentNullException.ThrowIfNull(allowedDownloadHosts);

        if (string.IsNullOrWhiteSpace(html) || allowedDownloadHosts.Count == 0)
        {
            return ArticleExtraction.Empty;
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);
        return ExtractLinks(document.DocumentNode, pageUri, allowedDownloadHosts);
    }

    // Anchors of a node towards allowed hosts, in document order, without duplicates.
    // Anchors naming a file (e.g. "Old.Boy.T02.pdf") on another host are kept aside as unsupported links.
    internal static ArticleExtraction ExtractLinks(HtmlNode root, Uri pageUri, IReadOnlyList<string> allowedDownloadHosts)
    {
        var anchors = root.SelectNodes(".//a[@href]");
        if (anchors is null)
        {
            return ArticleExtraction.Empty;
        }

        var links = new List<ExtractedLink>();
        var unsupportedLinks = new List<ExtractedLink>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var anchor in anchors)
        {
            var link = ToLink(anchor, pageUri);
            if (link is null || !seen.Add(link.Url))
            {
                continue;
            }

            if (link.Host.IsSameOrSubdomainOf(allowedDownloadHosts))
            {
                links.Add(link);
            }
            else if (link.FileName is not null && !link.Host.IsSameOrSubdomainOf([pageUri.Host]))
            {
                unsupportedLinks.Add(link);
            }
        }

        return new ArticleExtraction(links, unsupportedLinks);
    }

    internal static ExtractedLink? ToLink(HtmlNode anchor, Uri pageUri, string? groupKey = null)
    {
        var href = HtmlEntity.DeEntitize(anchor.GetAttributeValue("href", string.Empty)).Trim();
        if (href.Length == 0 ||
            !Uri.TryCreate(pageUri, href, out var url) ||
            (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp))
        {
            return null;
        }

        var text = HtmlEntity.DeEntitize(anchor.InnerText).Trim();
        if (IsProfileLink(url))
        {
            return null;
        }

        var surroundingText = HtmlEntity.DeEntitize(anchor.ParentNode?.InnerText ?? string.Empty);
        return new ExtractedLink(
            url.AbsoluteUri,
            url.Host,
            text,
            LinkMetadataParser.FindFileName(text, url),
            LinkMetadataParser.FindSizeBytes(text) ?? LinkMetadataParser.FindSizeBytes(surroundingText),
            groupKey);
    }

    // Hosters' "all my files" pages (e.g. https://fileq.net/users/xyz) are not downloads.
    private static bool IsProfileLink(Uri url)
    {
        var firstSegment = url.Segments.Length > 1 ? url.Segments[1].Trim('/') : string.Empty;
        return s_profileSegments.Contains(firstSegment);
    }

    private static readonly HashSet<string> s_profileSegments = new(["users", "user", "folder", "folders", "profile"], StringComparer.OrdinalIgnoreCase);
}

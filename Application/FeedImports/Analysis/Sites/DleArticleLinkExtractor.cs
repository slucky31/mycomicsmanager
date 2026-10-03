using System.Text.RegularExpressions;
using Application.Helpers;
using Domain.Extensions;
using HtmlAgilityPack;

namespace Application.FeedImports.Analysis.Sites;

// planete-bd.org and zone-ebook.com both run DataLife Engine with the same article layout:
// a post block holding the metadata (genre, tomes, sometimes the ISBN) and, after
// "Lien(s) de téléchargement", one anchor per file whose text is "File.Name.cbz - 101.5 MB".
// Only that block is read, so the sidebar, share buttons and ads never contribute links.
public abstract partial class DleArticleLinkExtractor : IDownloadLinkExtractor
{
    // "ISBN : 9782203211582", "ISBN ‏ : ‎ 978-2-203-21158-2"
    [GeneratedRegex(@"ISBN\D{0,12}((?:97[89][\d\- ]{10,16})|(?:\d[\d\- ]{8,12}[\dXx]))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IsbnPattern();

    private static Serilog.ILogger Log => Serilog.Log.ForContext<DleArticleLinkExtractor>();

    protected abstract string SiteDomain { get; }

    // XPath of the article block on this site.
    protected abstract string ArticleXPath { get; }

    public bool IsFallback => false;

    public bool CanHandle(Uri pageUri)
    {
        ArgumentNullException.ThrowIfNull(pageUri);
        return pageUri.Host.IsSameOrSubdomainOf([SiteDomain]);
    }

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
        var article = document.DocumentNode.SelectSingleNode(ArticleXPath)
                      ?? document.DocumentNode.SelectSingleNode("//*[@id='post-img']");
        if (article is null)
        {
            // The site layout changed: read the whole page rather than miss every link.
            Log.Warning("Feed import: article block not found on {Url}, falling back to the generic extractor", pageUri);
            return new ArticleExtraction(GenericDownloadLinkExtractor.ExtractLinks(document.DocumentNode, pageUri, allowedDownloadHosts));
        }

        var links = GenericDownloadLinkExtractor.ExtractLinks(article, pageUri, allowedDownloadHosts);
        return new ArticleExtraction(links, FindIsbn(HtmlEntity.DeEntitize(article.InnerText)));
    }

    private static string? FindIsbn(string text)
    {
        var match = IsbnPattern().Match(text);
        if (!match.Success)
        {
            return null;
        }

        var isbn = IsbnHelper.NormalizeIsbn(match.Groups[1].Value);
        return IsbnHelper.IsValidISBN(isbn) ? isbn : null;
    }

    protected static string ClassXPath(string element, string cssClass) =>
        $"//{element}[contains(concat(' ', normalize-space(@class), ' '), ' {cssClass} ')]";
}

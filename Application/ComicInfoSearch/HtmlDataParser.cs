using Ardalis.GuardClauses;
using HtmlAgilityPack;
using Serilog;

namespace Application.ComicInfoSearch;

public class HtmlDataParser
{
    private static ILogger Log => Serilog.Log.ForContext<HtmlDataParser>();

    private HtmlWeb Web { get; set; }

    protected HtmlDocument? Doc { get; set; }

    public HtmlDataParser()
    {
        Web = new HtmlWeb();
    }

    public bool LoadDocument(Uri url)
    {
        Log.Information("Load information from {Url}", url.ToString());
        Doc = Web.Load(url);
        return Doc != null;
    }

    public HtmlNode ExtractSingleNode(string htmlPath)
    {
        Guard.Against.Null(Doc, nameof(Doc), "Document is not loaded. Call LoadDocument method first.");
        return Doc.DocumentNode.SelectSingleNode(htmlPath);
    }

    public HtmlNode ExtractSingleNodeFromCssClass(string balise, string cssClass)
    {
        Guard.Against.Null(Doc, nameof(Doc), "Document is not loaded. Call LoadDocument method first.");
        return Doc.DocumentNode.SelectSingleNode("//" + balise + "[contains(@class, '" + cssClass + "')]");
    }

    public string ExtractTextValue(string htmlPath)
    {
        Guard.Against.Null(Doc, nameof(Doc), "Document is not loaded. Call LoadDocument method first.");
        var selectedNode = Doc.DocumentNode.SelectSingleNode(htmlPath);
        return ExtractTextValue(selectedNode);
    }

    public static string ExtractTextValue(HtmlNode selectedNode)
    {
        if (selectedNode is null)
        {
            return string.Empty;
        }
        return selectedNode.InnerText.Trim();
    }

    public string ExtractTextValueAndSplitOnSeparatorFromDocument(string htmlPath, string separator, int id)
    {
        var extractedText = ExtractTextValue(htmlPath);
        return ExtractTextValueAndSplitOnSeparator(extractedText, separator, id);
    }

    public static string ExtractTextValueAndSplitOnSeparatorFromNode(HtmlNode selectedNode, string separator, int id)
    {
        var extractedText = ExtractTextValue(selectedNode);
        return ExtractTextValueAndSplitOnSeparator(extractedText, separator, id);
    }

    private static string ExtractTextValueAndSplitOnSeparator(string extractedText, string separator, int id)
    {
        var splitExtractedText = extractedText;
        if (!string.IsNullOrEmpty(extractedText) && extractedText.Contains(separator, StringComparison.Ordinal))
        {
            splitExtractedText = extractedText.Split(separator)[id].Trim();
        }
        return splitExtractedText;
    }

    public string ExtractAttributValue(string htmlPath, string attribut)
    {
        Guard.Against.Null(Doc, nameof(Doc), "Document is not loaded. Call LoadDocument method first.");
        var selectedNode = Doc.DocumentNode.SelectSingleNode(htmlPath);
        if (selectedNode != null)
        {
            return selectedNode.Attributes[attribut].Value.Trim();
        }
        return string.Empty;
    }

    public string ExtractLinkHref(string patern)
    {
        Guard.Against.Null(Doc, nameof(Doc), "Document is not loaded. Call LoadDocument method first.");
        var nodes = Doc.DocumentNode.SelectNodes("//a[@href]");
        if (nodes is null)
        {
            return string.Empty;
        }

        var link = nodes
            .Select(n => n.Attributes["href"].Value)
            .FirstOrDefault(href => href.ToUpperInvariant().Trim().Contains(patern.ToUpperInvariant().Trim(), StringComparison.Ordinal));
        return link ?? string.Empty;
    }
}


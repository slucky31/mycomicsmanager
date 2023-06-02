using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using MyComicsManagerApi.Utils;
using Serilog;

namespace MyComicsManagerApi.DataParser
{
    public class HtmlDataParser
    {
        private static ILogger Log => Serilog.Log.ForContext<HtmlDataParser>();
        
        private HtmlWeb Web { get; set; }

        protected HtmlDocument Doc { get; set; }

        public HtmlDataParser()
        {
            Web = new HtmlWeb();
        }

        public void LoadDocument(string url)
        {
            Log.Here().Information("Load information from {Url}", url.Replace(Environment.NewLine, ""));
            Doc = Web.Load(url);
        }

        public HtmlNode ExtractSingleNode(string htmlPath)
        {
            return Doc.DocumentNode.SelectSingleNode(htmlPath);
        }
        
        public HtmlNode ExtractSingleNodeFromCssClass(string balise, string cssClass)
        {
            return Doc.DocumentNode.SelectSingleNode("//"+balise+"[contains(@class, '"+cssClass+"')]");
        }

        public string ExtractTextValue(string htmlPath)
        {
            var selectedNode = Doc.DocumentNode.SelectSingleNode(htmlPath);
            return ExtractTextValue(selectedNode);
        }
        
        public static string ExtractTextValue(HtmlNode selectedNode)
        {
            return selectedNode?.InnerText.Trim();
        }

        public string ExtractTextValueAndSplitOnSeparatorFromDocument(string htmlPath, string separator, int id)
        {
            var extractedText = ExtractTextValue(htmlPath);
            return ExtractTextValueAndSplitOnSeparator(extractedText, separator, id);
        }
        
        public string ExtractTextValueAndSplitOnSeparatorFromNode(HtmlNode selectedNode, string separator, int id)
        {
            var extractedText = ExtractTextValue(selectedNode);
            return ExtractTextValueAndSplitOnSeparator(extractedText, separator, id);
        }

        private string ExtractTextValueAndSplitOnSeparator(string extractedText, string separator, int id)
        {
            var splitExtractedText = extractedText;
            if (!string.IsNullOrEmpty(extractedText) && extractedText.Contains(separator))
            {
                splitExtractedText = extractedText.Split(separator)[id].Trim();
            }
            return splitExtractedText;
        }

        public string ExtractAttributValue(string htmlPath, string attribut)
        {
            var selectedNode = Doc.DocumentNode.SelectSingleNode(htmlPath);
            if (selectedNode != null)
            {
                return selectedNode.Attributes[attribut].Value.Trim();
            }
            return null;
        }
        
        public string ExtractLinkHref(string patern)
        {
            var nodes = Doc.DocumentNode.SelectNodes("//a[@href]");
            return nodes.Select(n => n.Attributes["href"].Value).FirstOrDefault(href => href.ToLowerInvariant().Trim().Contains(patern.ToLower().Trim()));
        }
    }
}
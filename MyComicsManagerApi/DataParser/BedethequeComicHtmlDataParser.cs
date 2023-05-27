using System.Collections.Generic;
using System.Linq;
using Google.Apis.CustomSearchAPI.v1.Data;
using Google.Apis.Services;
using MyComicsManagerApi.Settings;
using MyComicsManagerApi.Utils;
using Serilog;

namespace MyComicsManagerApi.DataParser;

public class BedethequeComicHtmlDataParser : ComicHtmlDataParser
{
    private static ILogger Log => Serilog.Log.ForContext<BedethequeComicHtmlDataParser>();
    
    private readonly IGoogleSearchSettings _googleSearchSettings;
    
    private Dictionary<string, string> ExtractedInfo { get; set; }
    
    private string FicheUrl { get; set; }
    
    private bool IsOneShot { get; set; }
    
    private int Rank { get; set; }
    
    public BedethequeComicHtmlDataParser(IGoogleSearchSettings googleSearchSettings)
    {
        _googleSearchSettings = googleSearchSettings;
        ExtractedInfo = new Dictionary<string, string>();
        IsOneShot = false;
    }
    
    protected override void Search(string isbn)
    {
        // Recherche sur Google via API car la recherche sur Bédéthèque est protégée
        // On recherche un lien qui contient __10000.html
        FicheUrl = SearchIsbnInGoogleBedethequeSearchEngine(isbn, "__10000.html");
        
        // Récupération de la page liée à l'ISBN recherché
        LoadDocument(FicheUrl);

        ExtractOneShot();
        
        // Récupération du tableau contenant les informations (les éléments sans valeurs ne sont pas affichés)
        ExtractDataTable();

        Rank = SearchRankFromIsbn(isbn);
    }

    protected int SearchRankFromIsbn(string isbn)
    {
        var key = ExtractedInfo.FirstOrDefault(x => x.Value == isbn).Key;
        return int.Parse(key.Split("-")[0]);
    }
    
    protected void ExtractDataTable()
    {
        ExtractedInfo.Clear();
        
        // Recherche de la liste des albums
        var listAlbumsNode = ExtractSingleNodeFromCssClass("ul","liste-albums");
        var albumNodes = listAlbumsNode.SelectNodes("./li");
        
        // Recherche de l'item ISBN dans la liste d'albums
        var rank = 1;
        foreach (var album in albumNodes)
        {
            var infoNodes = album.SelectNodes(".//div[2]/ul/li");
            foreach (var info in infoNodes)
            {
                var libelle = rank + "-" + ExtractTextValueAndSplitOnSeparatorFromNode(info, ":", 0);
                var valeur = ExtractTextValueAndSplitOnSeparatorFromNode(info, ":", 1);
                ExtractedInfo.Add(libelle, valeur);
            }
            rank++;
        }
    }
    
    private string SearchIsbnInGoogleBedethequeSearchEngine(string isbn, string pattern)
    {

        var apiKey = _googleSearchSettings.ApiKey;
        var cx = _googleSearchSettings.Cx;

        // Initialisation de l'appel au WebService
        var svc = new Google.Apis.CustomSearchAPI.v1.CustomSearchAPIService(new BaseClientService.Initializer { ApiKey = apiKey });
        var listRequest = svc.Cse.List();
        listRequest.Cx = cx;
        listRequest.Q = isbn;
        
        // Récupération des résultats
        IList<Result> paging = new List<Result>();
        var count = 0; 
        while (paging != null)
        {
            listRequest.Start = count * 10 + 1;
            paging = listRequest.Execute().Items; 
            if (paging != null)
            {
                foreach (var item in paging)
                {
                    Log.Here().Information("Link: {Link}", item.Link);
                    if (item.Link.ToLower().Trim().Contains(pattern.ToLower().Trim()))
                    {
                        return item.Link;
                    }
                }
            }
            count++;
        }
        return null;
    }
    
    protected override string ExtractColoriste()
    {
        return ExtractedInfo.GetValueOrDefault(Rank+"-Couleurs", "").Trim();
    }

    protected override string ExtractOneShot()
    {
        IsOneShot = "One shot".Equals(ExtractTextValue("/html/body/div[7]/div[2]/article/div[1]/div/h3/span[3]"));
        return IsOneShot.ToString();
    }
    
    protected override string ExtractTitre()
    {
        var xpath = "//ul[contains(@class, 'liste-albums')]/li[" + Rank + "]/div[2]/h3/a";
        return ExtractAttributValue(xpath, "title");
    }

    protected override string ExtractSerie()
    {
        if (IsOneShot)
        {
            return "One shot";
        }
        else
        {
            var xpath = "//div[contains(@class, 'bandeau-info')]/h1/a";
            return ExtractTextValue(xpath);
        }
    }

    protected override string ExtractSerieUrl()
    {
        return FicheUrl;
    }

    protected override string ExtractScenariste()
    {
        return ExtractedInfo.GetValueOrDefault(Rank+"-Scénario", "").Trim();
    }

    protected override string ExtractDessinateur()
    {
        return  ExtractedInfo.GetValueOrDefault(Rank+"-Dessin", "").Trim();
    }

    protected override string ExtractTome()
    {
        var xpath = "//ul[contains(@class, 'liste-albums')]/li[" + Rank + "]/div[2]/h3/a/span";
        return ExtractTextValueAndSplitOnSeparatorFromDocument(xpath,".",0).Trim();
    }

    protected override string ExtractDateParution()
    {
        return ExtractedInfo.GetValueOrDefault(Rank+"-Dépot légal", "").Trim();
    }

    protected override string ExtractIsbn()
    {
        return ExtractedInfo.GetValueOrDefault(Rank+"-ISBN", "").Trim();
    }

    protected override string ExtractUrl()
    {
        var xpath = "//ul[contains(@class, 'liste-albums')]/li[" + Rank + "]/div[2]/h3/a";
        return ExtractAttributValue(xpath, "href");
    }

    protected override string ExtractEditeur()
    {
        return ExtractedInfo.GetValueOrDefault(Rank+"-Editeur", "").Trim();
    }

    protected override string ExtractNote()
    {
        var xpath = "//ul[contains(@class, 'liste-albums')]/li[" + Rank + "]/div[2]/div/div/p/strong";
        return ExtractTextValue(xpath).Trim();
    }

    protected override string ExtractLangage()
    {
        return "Français";
    }

    protected override string ExtractPrix()
    {
        return ExtractedInfo.GetValueOrDefault(Rank+"-Estimation", "").Trim();
    }

    protected override string ExtractSerieStatus()
    {
        // TODO : à rendre plus lisible et à gérer au niveau de la série
        return ExtractTextValue("/html/body/div[7]/div[2]/article/div[1]/div/h3/span[3]");   
    }
}
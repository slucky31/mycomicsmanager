using System.Globalization;

namespace Application.ComicInfoSearch;

public class BedethequeComicHtmlDataParser : ComicHtmlDataParser
{
    private static class FieldKeys
    {
        public const string Couleurs = "Couleurs";
        public const string Scenario = "Scénario";
        public const string Dessin = "Dessin";
        public const string ISBN = "ISBN";
        public const string DepotLegal = "Dépot légal";
        public const string Editeur = "Editeur";
        public const string Estimation = "Estimation";
    }

    private readonly IGoogleSearchService _searchService;

    private Dictionary<string, string> ExtractedInfo { get; set; }

    private Uri? FicheUrl { get; set; }

    private bool IsOneShot { get; set; }

    private int Rank { get; set; }

    public BedethequeComicHtmlDataParser(IGoogleSearchService searchService)
    {
        _searchService = searchService;
        ExtractedInfo = new Dictionary<string, string>();
        IsOneShot = false;
    }

    protected override bool Search(string isbn)
    {
        // Recherche sur Google via API car la recherche sur Bédéthèque est protégée
        // On recherche un lien qui contient l'isbn
        SearchSerieDataFromKeyWordAndUrlPattern(isbn);
        Rank = SearchRankFromIsbn(isbn);
        return Rank != 0;
    }

    protected override bool SearchSerie(string serie, int tome)
    {
        // Recherche sur Google via API car la recherche sur Bédéthèque est protégée
        // On recherche un lien qui contient l'isbn
        if (ExtractedInfo.Count == 0)
        {
            SearchSerieDataFromKeyWordAndUrlPattern(serie);
        }
        Rank = tome;
        return Rank != 0;
    }

    protected override bool SearchSerieFromUrl(Uri url, int tome)
    {
        // Recherche sur Google via API car la recherche sur Bédéthèque est protégée
        // On recherche un lien qui contient l'isbn
        if (ExtractedInfo.Count == 0)
        {
            FicheUrl = url;
            ExtractComicDataFromUrl();
        }
        Rank = tome;
        return Rank != 0;
    }

    private void SearchSerieDataFromKeyWordAndUrlPattern(string keyword)
    {
        FicheUrl = new Uri(_searchService.SearchLinkFromKeywordAndPattern(keyword));
        ExtractComicDataFromUrl();
    }

    private void ExtractComicDataFromUrl()
    {
        // Récupération de la page liée à la série recherchée
        LoadDocument(FicheUrl!);

        ExtractOneShot();

        // Récupération du tableau contenant les informations (les éléments sans valeurs ne sont pas affichés)
        ExtractDataTable();
    }

    private int SearchRankFromIsbn(string isbn)
    {
        var entry = ExtractedInfo.FirstOrDefault(x => x.Value == isbn);
        if (entry.Key is null)
        {
            return 0;
        }
        return int.Parse(entry.Key.Split("-")[0], CultureInfo.InvariantCulture);
    }

    protected void ExtractDataTable()
    {
        ExtractedInfo.Clear();

        // Recherche de la liste des albums
        var listAlbumsNode = ExtractSingleNodeFromCssClass("ul", "liste-albums");
        if (listAlbumsNode is null)
        {
            return;
        }

        var albumNodes = listAlbumsNode.SelectNodes("./li");
        if (albumNodes is null)
        {
            return;
        }

        // Recherche de l'item ISBN dans la liste d'albums
        var rank = 1;
        foreach (var album in albumNodes)
        {
            var infoNodes = album.SelectNodes(".//div[2]/ul/li");
            if (infoNodes is not null)
            {
                foreach (var info in infoNodes)
                {
                    var libelle = rank + "-" + ExtractTextValueAndSplitOnSeparatorFromNode(info, ":", 0);
                    var valeur = ExtractTextValueAndSplitOnSeparatorFromNode(info, ":", 1);
                    ExtractedInfo.Add(libelle, valeur);
                }
            }
            rank++;
        }
    }

    protected override string ExtractColoriste()
    {
        return ExtractedInfo.GetValueOrDefault(Rank + "-" + FieldKeys.Couleurs, "").Trim();
    }

    protected override string ExtractOneShot()
    {
        IsOneShot = "One shot".Equals(ExtractTextValue("/html/body/div[7]/div[2]/article/div[1]/div/h3/span[3]"), StringComparison.Ordinal);
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

        var xpath = "//div[contains(@class, 'bandeau-info')]/h1/a";
        return ExtractTextValue(xpath);
    }

    protected override Uri? ExtractSerieUrl()
    {
        return FicheUrl;
    }

    protected override string ExtractScenariste()
    {
        return ExtractedInfo.GetValueOrDefault(Rank + "-" + FieldKeys.Scenario, "").Trim();
    }

    protected override string ExtractDessinateur()
    {
        return ExtractedInfo.GetValueOrDefault(Rank + "-" + FieldKeys.Dessin, "").Trim();
    }

    protected override string ExtractTome()
    {
        var xpath = "//ul[contains(@class, 'liste-albums')]/li[" + Rank + "]/div[2]/h3/a/span";
        return ExtractTextValueAndSplitOnSeparatorFromDocument(xpath, ".", 0).Trim();
    }

    protected override string ExtractDateParution()
    {
        return ExtractedInfo.GetValueOrDefault(Rank + "-" + FieldKeys.DepotLegal, "").Trim();
    }

    protected override string ExtractIsbn()
    {
        return ExtractedInfo.GetValueOrDefault(Rank + "-" + FieldKeys.ISBN, "").Trim();
    }

    protected override Uri ExtractUrl()
    {
        var xpath = "//ul[contains(@class, 'liste-albums')]/li[" + Rank + "]/div[2]/h3/a";
        return new Uri(ExtractAttributValue(xpath, "href"));
    }

    protected override string ExtractEditeur()
    {
        return ExtractedInfo.GetValueOrDefault(Rank + "-" + FieldKeys.Editeur, "").Trim();
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
        return ExtractedInfo.GetValueOrDefault(Rank + "-" + FieldKeys.Estimation, "").Trim();
    }

    protected override string ExtractSerieStatus()
    {
        return ExtractTextValue("/html/body/div[7]/div[2]/article/div[1]/div/h3/span[3]");
    }
}


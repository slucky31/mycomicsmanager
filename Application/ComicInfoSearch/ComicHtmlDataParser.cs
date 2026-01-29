using Serilog;

namespace Application.ComicInfoSearch;

public enum ComicDataEnum
{
    TITRE,
    SERIE,
    SERIE_URL,
    SCENARISTE,
    DESSINATEUR,
    COLORISTE,
    TOME,
    DATE_PARUTION,
    ISBN,
    URL,
    EDITEUR,
    NOTE,
    FILE,
    ONESHOT,
    VIGNETTE,
    LANGAGE,
    PRIX
}

public abstract class ComicHtmlDataParser : HtmlDataParser
{
    private static ILogger Log => Serilog.Log.ForContext<ComicHtmlDataParser>();

    private Dictionary<ComicDataEnum, string> ExtractedData { get; set; }

    protected ComicHtmlDataParser()
    {
        ExtractedData = new Dictionary<ComicDataEnum, string>();
    }

    private void ExtractData()
    {
        ExtractedData.Add(ComicDataEnum.TITRE, ExtractTitre());
        ExtractedData.Add(ComicDataEnum.SERIE, ExtractSerie());

        var serieUrl = ExtractSerieUrl();
        ExtractedData.Add(ComicDataEnum.SERIE_URL, serieUrl != null ? serieUrl.ToString() : string.Empty);

        ExtractedData.Add(ComicDataEnum.SCENARISTE, ExtractScenariste());
        ExtractedData.Add(ComicDataEnum.DESSINATEUR, ExtractDessinateur());
        ExtractedData.Add(ComicDataEnum.COLORISTE, ExtractColoriste());
        ExtractedData.Add(ComicDataEnum.TOME, ExtractTome());
        ExtractedData.Add(ComicDataEnum.DATE_PARUTION, ExtractDateParution());
        ExtractedData.Add(ComicDataEnum.ISBN, ExtractIsbn());

        var url = ExtractUrl();
        ExtractedData.Add(ComicDataEnum.URL, url != null ? url.ToString() : string.Empty);

        ExtractedData.Add(ComicDataEnum.EDITEUR, ExtractEditeur());
        ExtractedData.Add(ComicDataEnum.NOTE, ExtractNote());
        ExtractedData.Add(ComicDataEnum.ONESHOT, ExtractOneShot());
        ExtractedData.Add(ComicDataEnum.LANGAGE, ExtractLangage());
        ExtractedData.Add(ComicDataEnum.PRIX, ExtractPrix());
    }

    public Dictionary<ComicDataEnum, string> SearchComicInfoFromIsbn(string isbn)
    {
        ExtractedData.Clear();
        var result = Search(isbn);
        if (!result)
        {
            Log.Warning("Aucune donnée n'a été remontée de la recherche !");
            return ExtractedData;
        }

        ExtractData();
        return ExtractedData;
    }

    public Dictionary<ComicDataEnum, string> SearchComicInfoFromSerie(string serie, int tome)
    {
        ExtractedData.Clear();

        var result = SearchSerie(serie, tome);
        if (!result)
        {
            Log.Warning("Aucune donnée n'a été remontée de la recherche !");
            return ExtractedData;
        }

        ExtractData();
        return ExtractedData;
    }

    public Dictionary<ComicDataEnum, string> SearchComicInfoFromSerieUrl(Uri url, int tome)
    {
        ExtractedData.Clear();

        var result = SearchSerieFromUrl(url, tome);
        if (!result)
        {
            Log.Warning("Aucune donnée n'a été remontée de la recherche !");
            return ExtractedData;
        }

        ExtractData();
        return ExtractedData;
    }

    protected abstract string ExtractColoriste();

    protected abstract string ExtractOneShot();

    protected abstract bool Search(string isbn);

    protected abstract bool SearchSerie(string serie, int tome);

    protected abstract bool SearchSerieFromUrl(Uri url, int tome);

    protected abstract string ExtractTitre();

    protected abstract string ExtractSerie();

    protected abstract Uri? ExtractSerieUrl();

    protected abstract string ExtractScenariste();

    protected abstract string ExtractDessinateur();

    protected abstract string ExtractTome();

    protected abstract string ExtractDateParution();

    protected abstract string ExtractIsbn();

    protected abstract Uri? ExtractUrl();

    protected abstract string ExtractEditeur();

    protected abstract string ExtractNote();

    protected abstract string ExtractLangage();

    protected abstract string ExtractPrix();

    protected abstract string ExtractSerieStatus();
}


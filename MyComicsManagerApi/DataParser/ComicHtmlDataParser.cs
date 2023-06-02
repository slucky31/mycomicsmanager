using System;
using System.Collections.Generic;
using Serilog;

namespace MyComicsManagerApi.DataParser
{
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
            ExtractedData.Add(ComicDataEnum.SERIE_URL, ExtractSerieUrl());
            ExtractedData.Add(ComicDataEnum.SCENARISTE, ExtractScenariste());
            ExtractedData.Add(ComicDataEnum.DESSINATEUR, ExtractDessinateur());
            ExtractedData.Add(ComicDataEnum.COLORISTE, ExtractColoriste());
            ExtractedData.Add(ComicDataEnum.TOME, ExtractTome());
            ExtractedData.Add(ComicDataEnum.DATE_PARUTION, ExtractDateParution());
            ExtractedData.Add(ComicDataEnum.ISBN, ExtractIsbn());
            ExtractedData.Add(ComicDataEnum.URL, ExtractUrl());
            ExtractedData.Add(ComicDataEnum.EDITEUR, ExtractEditeur());
            ExtractedData.Add(ComicDataEnum.NOTE, ExtractNote());
            ExtractedData.Add(ComicDataEnum.ONESHOT, ExtractOneShot());
            ExtractedData.Add(ComicDataEnum.LANGAGE, ExtractLangage());
            ExtractedData.Add(ComicDataEnum.PRIX, ExtractPrix());
        }

        public Dictionary<ComicDataEnum, string> SearchComicInfoFromIsbn(string isbn)
        {
            ExtractedData.Clear();
            try
            {
                
                Search(isbn);
                ExtractData();
            }
            catch (Exception e)
            {
                Log.Warning(e, "Aucune donnée n'a été remontée de la recherche !");
                return ExtractedData;
            }
            return ExtractedData;
        }
        
        public Dictionary<ComicDataEnum, string> SearchComicInfoFromSerie(string serie, int tome)
        {
            ExtractedData.Clear();
            try
            {
                SearchSerie(serie, tome);
                ExtractData();
            }
            catch (Exception e)
            {
                Log.Warning(e, "Aucune donnée n'a été remontée de la recherche !");
                return ExtractedData;
            }
            return ExtractedData;
        }
        
        public Dictionary<ComicDataEnum, string> SearchComicInfoFromSerieUrl(string url, int tome)
        {
            ExtractedData.Clear();
            try
            {
                SearchSerieFromUrl(url, tome);
                ExtractData();
            }
            catch (Exception e)
            {
                Log.Warning(e, "Aucune donnée n'a été remontée de la recherche !");
                return ExtractedData;
            }
            return ExtractedData;
        }

        protected abstract string ExtractColoriste();

        protected abstract string ExtractOneShot();

        protected abstract void Search(string isbn);

        protected abstract void SearchSerie(string serie, int tome);

        protected abstract void SearchSerieFromUrl(string url, int tome);

        protected abstract string ExtractTitre();

        protected abstract string ExtractSerie();

        protected abstract string ExtractSerieUrl();

        protected abstract string ExtractScenariste();

        protected abstract string ExtractDessinateur();

        protected abstract string ExtractTome();

        protected abstract string ExtractDateParution();

        protected abstract string ExtractIsbn();

        protected abstract string ExtractUrl();

        protected abstract string ExtractEditeur();

        protected abstract string ExtractNote();
        
        protected abstract string ExtractLangage();
        
        protected abstract string ExtractPrix();

        protected abstract string ExtractSerieStatus();
    }
}
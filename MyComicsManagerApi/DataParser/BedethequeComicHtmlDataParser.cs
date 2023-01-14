using System;
using System.Collections.Generic;
using Google.Apis.CustomSearchAPI.v1.Data;
using Google.Apis.Services;
using MyComicsManagerApi.Exceptions;
using MyComicsManagerApi.Utils;
using Serilog;

namespace MyComicsManagerApi.DataParser;

public class BedethequeComicHtmlDataParser : ComicHtmlDataParser
{
    private static ILogger Log => Serilog.Log.ForContext<BedethequeComicHtmlDataParser>();
    
    protected override void Search(string isbn)
    {
        // Recherche sur Google via API car la recherche sur Bedetheque est protégée
        // On recherche un lien qui contient __10000.html
        var link = SearchISBNInGoogleBedethequeSearchEngine(isbn, "BD-");
        
    }

    private string SearchISBNInGoogleBedethequeSearchEngine(string isbn, string pattern)
    {
        // https://console.cloud.google.com/apis/credentials
        string apiKey = "AIzaSyBr0mcT91wbhxovndMEW2vlMNgIDCe3SS8";
        // https://programmablesearchengine.google.com/controlpanel/all
        string cx = "a094949e8b0b44aa7";

        var svc = new Google.Apis.CustomSearchAPI.v1.CustomSearchAPIService(new BaseClientService.Initializer { ApiKey = apiKey });
        var listRequest = svc.Cse.List();
        listRequest.Cx = cx;
        listRequest.Q = isbn;
            
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
                    Log.Here().Information("Link: {0}", item.Link);
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
        throw new System.NotImplementedException();
    }

    protected override string ExtractOneShot()
    {
        throw new System.NotImplementedException();
    }
    
    protected override string ExtractTitre()
    {
        throw new System.NotImplementedException();
    }

    protected override string ExtractSerie()
    {
        throw new System.NotImplementedException();
    }

    protected override string ExtractSerieUrl()
    {
        throw new System.NotImplementedException();
    }

    protected override string ExtractScenariste()
    {
        throw new System.NotImplementedException();
    }

    protected override string ExtractDessinateur()
    {
        throw new System.NotImplementedException();
    }

    protected override string ExtractTome()
    {
        throw new System.NotImplementedException();
    }

    protected override string ExtractDateParution()
    {
        throw new System.NotImplementedException();
    }

    protected override string ExtractISBN()
    {
        throw new System.NotImplementedException();
    }

    protected override string ExtractURL()
    {
        throw new System.NotImplementedException();
    }

    protected override string ExtractEditeur()
    {
        throw new System.NotImplementedException();
    }

    protected override string ExtractNote()
    {
        throw new System.NotImplementedException();
    }

    protected override string ExtractLangage()
    {
        throw new System.NotImplementedException();
    }

    protected override string ExtractPrix()
    {
        throw new System.NotImplementedException();
    }

    protected override string ExtractSerieStatus()
    {
        throw new System.NotImplementedException();
    }
}
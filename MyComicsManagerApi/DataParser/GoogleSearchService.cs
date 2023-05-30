using System.Collections.Generic;
using Google.Apis.CustomSearchAPI.v1.Data;
using Google.Apis.Services;
using MyComicsManagerApi.Settings;
using MyComicsManagerApi.Utils;
using Serilog;

namespace MyComicsManagerApi.DataParser;

public class GoogleSearchService : IGoogleSearchService
{
    private static ILogger Log => Serilog.Log.ForContext<GoogleSearchService>();
    
    private readonly IGoogleSearchSettings _googleSearchSettings;

    public GoogleSearchService(IGoogleSearchSettings googleSearchSettings)
    {
        _googleSearchSettings = googleSearchSettings;
    }
    
    public string SearchLinkFromIsbnAndPattern(string isbn, string pattern)
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
}

public interface IGoogleSearchService
{
    public string SearchLinkFromIsbnAndPattern(string isbn, string pattern);
}
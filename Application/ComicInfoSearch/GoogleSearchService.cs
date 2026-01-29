namespace Application.ComicInfoSearch;

public class GoogleSearchService : IGoogleSearchService
{
    private readonly ICustomSearchApiClient _searchApiClient;

    public GoogleSearchService(ICustomSearchApiClient searchApiClient)
    {
        _searchApiClient = searchApiClient;
    }

    public string SearchLinkFromKeywordAndPattern(string keyword)
    {
        var count = 0;
        var links = _searchApiClient.ExecuteSearch(keyword, count * 10 + 1);
        while (links is not null)
        {
            if (links.Count > 0)
            {
                return links.First();
            }
            count++;
            links = _searchApiClient.ExecuteSearch(keyword, count * 10 + 1);
        }
        return string.Empty;
    }
}

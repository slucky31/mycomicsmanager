namespace Application.ComicInfoSearch;

public interface ICustomSearchApiClient
{
    IList<string>? ExecuteSearch(string keyword, int startIndex);
}

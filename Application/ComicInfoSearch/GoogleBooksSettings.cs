namespace Application.ComicInfoSearch;

public class GoogleBooksSettings
{
    public Uri? BaseUrl { get; set; }

    // Without a key, the requests share Google's anonymous quota, which is usually exhausted (429).
    public string? ApiKey { get; set; }
}

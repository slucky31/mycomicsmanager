namespace Application.ComicInfoSearch;

public sealed class BubbleSettings
{
    // The site, whose album pages give the publisher, the date, the pages and the authors.
    public required Uri BaseUrl { get; init; }

    // The Algolia search the site uses, searched by ISBN with its public search-only key.
    public required Uri AlgoliaBaseUrl { get; init; }
    public string ApplicationId { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
}

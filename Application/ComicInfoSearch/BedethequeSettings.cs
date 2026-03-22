namespace Application.ComicInfoSearch;

public sealed class BedethequeSettings
{
    public string SerpApiKey { get; init; } = string.Empty;
    public Uri BaseUrl { get; init; } = new Uri("https://www.bedetheque.com");
    public Uri SerpApiBaseUrl { get; init; } = new Uri("https://serpapi.com");
}


namespace Application.ComicInfoSearch;

public sealed class BedethequeSettings
{
    public string SerpApiKey { get; init; } = string.Empty;
    public required Uri BaseUrl { get; init; } 
    public required Uri SerpApiBaseUrl { get; init; } 
}


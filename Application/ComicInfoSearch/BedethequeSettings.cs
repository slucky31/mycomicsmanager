namespace Application.ComicInfoSearch;

public sealed class BedethequeSettings
{
    public string SerpApiKey { get; init; } = string.Empty;
    public required Uri BaseUrl { get; init; }
    public required Uri SerpApiBaseUrl { get; init; }

    // Bedetheque is behind Cloudflare, which may block the server's requests: it can be turned off.
    public bool Enabled { get; init; } = true;

    // After a Cloudflare challenge, Bedetheque is not searched for this long (each search costs SerpApi calls).
    public int CloudflarePauseHours { get; init; } = 24;
}

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

    // Cloudflare Browser Run loads the pages in a real browser, which Bedetheque lets through
    // when it blocks the server's own requests: used when both the account and the token are set.
    public string CloudflareAccountId { get; init; } = string.Empty;
    public string CloudflareApiToken { get; init; } = string.Empty;
    public Uri BrowserRunBaseUrl { get; init; } = new("https://api.cloudflare.com/client/v4");

    public bool UseBrowserRun =>
        !string.IsNullOrWhiteSpace(CloudflareAccountId) && !string.IsNullOrWhiteSpace(CloudflareApiToken);
}

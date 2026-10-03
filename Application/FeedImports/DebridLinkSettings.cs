namespace Application.FeedImports;

public class DebridLinkSettings
{
    public Uri BaseUrl { get; set; } = new("https://debrid-link.com/api/v2/");

    // Private API key generated in the Debrid-Link account; empty = downloads are not started.
    public string ApiKey { get; set; } = string.Empty;

    // Mirrors are tried in this order (subdomains included); hosts not listed come last, in page order.
    public IReadOnlyList<string> HosterPriority { get; set; } = [];

    // Domains serving the unlocked files (downloadUrl), in addition to FeedImport:AllowedDownloadHosts.
    public IReadOnlyList<string> DownloadHosts { get; set; } = [];
}

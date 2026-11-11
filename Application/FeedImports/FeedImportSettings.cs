namespace Application.FeedImports;

public class FeedImportSettings
{
    public bool Enabled { get; set; }

    public int SyncIntervalMinutes { get; set; } = 30;

    // Email of the MCM user who owns the decisions created by the background sync.
    public string UserEmail { get; set; } = string.Empty;

    public IReadOnlyList<string> AllowedSourceHosts { get; set; } = [];

    public IReadOnlyList<string> AllowedDownloadHosts { get; set; } = [];

    // A file server that keeps the connection open but stops sending data fails the download after this delay.
    public TimeSpan DownloadIdleTimeout { get; set; } = TimeSpan.FromSeconds(60);

    // Maximum duration of one file download (HttpClient.Timeout does not cover the body).
    // Shorter than FeedImportConstants.StaleDownloadDelay, so a running download is never retried.
    public TimeSpan DownloadTimeout { get; set; } = TimeSpan.FromMinutes(20);
}

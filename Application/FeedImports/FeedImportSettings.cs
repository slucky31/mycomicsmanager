namespace Application.FeedImports;

public class FeedImportSettings
{
    public bool Enabled { get; set; }

    public int SyncIntervalMinutes { get; set; } = 30;

    // Email of the MCM user who owns the decisions created by the background sync.
    public string UserEmail { get; set; } = string.Empty;

    public string TargetLibraryName { get; set; } = "À trier";

    public IReadOnlyList<string> AllowedSourceHosts { get; set; } = [];

    public IReadOnlyList<string> AllowedDownloadHosts { get; set; } = [];
}

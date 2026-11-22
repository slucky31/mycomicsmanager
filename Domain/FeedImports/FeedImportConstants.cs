namespace Domain.FeedImports;

public static class FeedImportConstants
{
    public const int MaxEntryTitleLength = 500;
    public const int MaxEntryUrlLength = 2000;
    public const int MaxParsedSerieLength = 200;
    public const int MaxParsedTitleLength = 300;
    public const int MaxChosenMirrorLength = 2000;
    public const int MaxReasonLength = 1000;
    public const int MaxErrorMessageLength = 2000;
    public const int MaxErrorStepLength = 100;
    public const int MaxEventDescriptionLength = 1000;
    public const int MaxHostLength = 255;

    // The host filter of the decisions without any link (a real host always holds a dot).
    public const string NoHost = "-";

    // A download still "Downloading" after this delay was interrupted (restart, crash): it can be retried.
    // Longer than the download timeout of one mirror (FeedImport:DownloadTimeout, 20 min), and the decision
    // is saved after each failed mirror, so a running download is never retried.
    public static readonly TimeSpan StaleDownloadDelay = TimeSpan.FromMinutes(30);

    // ErrorStep of a failed download, and of a download whose mirrors are all on hosts down on Debrid-Link.
    public const string DownloadStep = "Download";
    public const string HostDownStep = "Host";

    // A decision waiting for a host or for the Debrid-Link quota is retried by the sync at most this many times
    // (counted since the last user action on it).
    public const int MaxAutoRetries = 3;

    // The Debrid-Link quotas are daily: a decision that failed on a quota is retried once this delay is over.
    public static readonly TimeSpan QuotaRetryDelay = TimeSpan.FromHours(24);

    // Host state changes older than this are deleted, except the latest one of each host.
    public static readonly TimeSpan HostStatusRetention = TimeSpan.FromDays(90);
}

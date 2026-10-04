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

    // A download still "Downloading" after this delay was interrupted (restart, crash): it can be retried.
    // Longer than the download timeout, so a running download is never retried.
    public static readonly TimeSpan StaleDownloadDelay = TimeSpan.FromHours(1);
}

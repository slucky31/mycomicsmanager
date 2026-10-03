namespace Application.FeedImports.Download;

// Getting the file: Debrid-Link unlocks the hoster link, the downloader streams it into the temp directory.
public sealed record FeedImportDownloadServices(IDebridLinkClient DebridLinkClient, IFeedImportFileDownloader FileDownloader);

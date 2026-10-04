using Domain.Primitives;

namespace Domain.FeedImports;

public static class FeedImportError
{
    public static readonly TError BadRequest = new("FEED400", "Verify the feed import request parameters.");
    public static readonly TError Disabled = new("FEED403", "Feed import is disabled (FeedImport:Enabled).");
    public static readonly TError NotFound = new("FEED404", "Feed import decision not found.");
    public static readonly TError CategoryNotFound = new("FEED404C", "Miniflux category not found.");
    public static readonly TError InvalidStatusTransition = new("FEED405", "Invalid feed import decision status transition.");
    public static readonly TError SourceHostNotAllowed = new("FEED403S", "Source domain not allowed.");
    public static readonly TError PageUnavailable = new("FEED502P", "The article page could not be retrieved.");
    public static readonly TError DeleteInProgress = new("FEED409B", "Download or import in progress: the decision cannot be deleted.");
    public static readonly TError Duplicate = new("FEED409", "A decision already exists for this Miniflux entry.");
    public static readonly TError DebridLinkUnavailable = new("FEED502D", "Debrid-Link could not be reached or returned an error.");
    public static readonly TError DebridLinkUnauthorized = new("FEED401D", "Debrid-Link API key is missing or invalid (DebridLink:ApiKey).");
    public static readonly TError DebridLinkQuotaReached = new("FEED429D", "Debrid-Link quota reached.");
    public static readonly TError DebridLinkFileUnavailable = new("FEED410D", "File unavailable on the host (dead or deleted link).");
    public static readonly TError DebridLinkHostQuotaReached = new("FEED429H", "Debrid-Link quota reached for this host.");
    public static readonly TError DebridLinkHostNotSupported = new("FEED422D", "Host not supported by Debrid-Link.");
    public static readonly TError DownloadHostNotAllowed = new("FEED403H", "Download domain not allowed.");
    public static readonly TError DownloadFailed = new("FEED502F", "The file download failed.");
    public static readonly TError FileTooLarge = new("FEED413", "File too large (Import:MaxFileSizeMb).");
    public static readonly TError UnsupportedFileType = new("FEED415", "Unsupported file type (Import:SupportedExtensions).");
    public static readonly TError TargetLibraryInvalid = new("FEED409L", "The target library exists but is not a digital library.");
    public static readonly TError MinifluxUnavailable = new("FEED502", "Miniflux could not be reached or returned an error.");
    public static readonly TError MinifluxUnauthorized = new("FEED401M", "Miniflux API key is missing or invalid (Miniflux:ApiKey).");
}

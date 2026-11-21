namespace Domain.FeedImports;

// Why a failed decision can be retried by the sync without the user.
public enum FeedImportRetryKind
{
    None,     // Real failure (dead link, file too large...): only the user can retry it
    HostDown, // Every mirror was on a host down on (or not reachable through) Debrid-Link: retried once a host is back up
    Quota     // Debrid-Link quota reached: retried once FeedImportConstants.QuotaRetryDelay is over
}

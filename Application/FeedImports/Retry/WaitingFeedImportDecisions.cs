using Application.Interfaces;
using Domain.FeedImports;

namespace Application.FeedImports.Retry;

// The failed decisions waiting for a host or the quota that can be retried now, with the host states used to decide.
internal static class WaitingFeedImportDecisions
{
    public static async Task<(IReadOnlyList<FeedImportDecision> Due, Func<string, bool> IsHostUp)> GetDueAsync(
        IFeedImportDecisionRepository decisionRepository,
        IDebridLinkClient debridLinkClient,
        TimeProvider timeProvider,
        Guid userId,
        FeedImportDecidedBy decidedBy,
        CancellationToken cancellationToken)
    {
        var failed = await decisionRepository.GetByStatusAsync(userId, FeedImportDecisionStatus.Failed, cancellationToken);
        var waiting = failed.Where(d => d.RetryKind != FeedImportRetryKind.None).ToList();
        if (waiting.Count == 0)
        {
            return ([], _ => false);
        }

        // Read only when needed: the Debrid-Link status page is called once per run.
        var statuses = waiting.Exists(d => d.RetryKind == FeedImportRetryKind.HostDown)
            ? await debridLinkClient.GetHostStatusesAsync(cancellationToken)
            : DebridLinkHostStatuses.Unknown;
        Func<string, bool> isHostUp = statuses.IsKnownUp;
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var due = waiting
            .Where(d => decidedBy == FeedImportDecidedBy.Auto ? d.CanRetryAutomatically(isHostUp, utcNow) : d.IsRetryDue(isHostUp, utcNow))
            .ToList();
        return (due, isHostUp);
    }
}

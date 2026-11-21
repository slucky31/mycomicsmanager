using Application.Abstractions.Messaging;
using Domain.FeedImports;

namespace Application.FeedImports.Retry;

// Retries the failed decisions waiting for a host or for the Debrid-Link quota (FeedImportDecision.RetryKind).
// Auto: by the sync, at most FeedImportConstants.MaxAutoRetries times. User: the "Retry these" button, no limit.
public record RetryWaitingFeedImportDecisionsCommand(Guid UserId, FeedImportDecidedBy DecidedBy) : ICommand<IReadOnlyList<RetriedFeedImportDecision>>;

// Status reached after the retry: LinksExtracted is downloaded next, Pending is analyzed again by the sync.
public sealed record RetriedFeedImportDecision(Guid Id, FeedImportDecisionStatus Status);

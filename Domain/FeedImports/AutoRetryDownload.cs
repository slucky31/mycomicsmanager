using Domain.Primitives;

namespace Domain.FeedImports;

// A feed downloaded after at least one automatic retry (#1318). Kept apart from the decision so the
// count survives when the decision is deleted from the Feeds page.
public class AutoRetryDownload : Entity<Guid>
{
    public Guid UserId { get; private set; }

    // The decision downloaded; it may have been deleted since.
    public Guid DecisionId { get; private set; }

    public int AutoRetryCount { get; private set; }

    public DateTime DownloadedAt { get; private set; }

    protected AutoRetryDownload() { }

    // Null when the decision was not retried automatically.
    public static AutoRetryDownload? From(FeedImportDecision decision, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.AutoRetryCount == 0)
        {
            return null;
        }

        return new AutoRetryDownload
        {
            Id = Guid.CreateVersion7(),
            UserId = decision.UserId,
            DecisionId = decision.Id,
            AutoRetryCount = decision.AutoRetryCount,
            DownloadedAt = Clock.UtcNow(timeProvider)
        };
    }
}

using Domain.Primitives;

namespace Domain.FeedImports;

public class FeedImportDecisionEvent : Entity<Guid>
{
    public Guid FeedImportDecisionId { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public FeedImportDecisionStatus? PreviousStatus { get; private set; }

    public FeedImportDecisionStatus Status { get; private set; }

    public FeedImportDecidedBy DecidedBy { get; private set; }

    public string Description { get; private set; } = string.Empty;

    protected FeedImportDecisionEvent() { }

    internal static FeedImportDecisionEvent Create(
        Guid feedImportDecisionId,
        DateTime occurredAt,
        FeedImportDecisionStatus? previousStatus,
        FeedImportDecisionStatus status,
        FeedImportDecidedBy decidedBy,
        string description) => new()
        {
            Id = Guid.CreateVersion7(),
            FeedImportDecisionId = feedImportDecisionId,
            OccurredAt = occurredAt,
            PreviousStatus = previousStatus,
            Status = status,
            DecidedBy = decidedBy,
            Description = description
        };
}

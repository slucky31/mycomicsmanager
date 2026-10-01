using Domain.Primitives;

namespace Domain.FeedImports;

#pragma warning disable CA1054, CA1056 // URL stored as string in database

public class FeedImportDecision : Entity<Guid>
{
    public const string CreatedReason = "Article enregistré depuis Miniflux.";

    public Guid UserId { get; private set; }

    public long MinifluxEntryId { get; private set; }

    public string EntryTitle { get; private set; } = string.Empty;

    public string EntryUrl { get; private set; } = string.Empty;

    public DateTime? PublishedAt { get; private set; }

    public string? ParsedSerie { get; private set; }

    public string? ParsedTitle { get; private set; }

    public int? ParsedVolume { get; private set; }

    public string? Links { get; private set; }

    public string? ChosenMirror { get; private set; }

    public Guid? MatchedBookId { get; private set; }

    public Guid? ImportJobId { get; private set; }

    public Guid? DigitalBookId { get; private set; }

    public FeedImportDecisionStatus Status { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public FeedImportDecidedBy DecidedBy { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? ErrorStep { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private readonly List<FeedImportDecisionEvent> _events = [];
    public IReadOnlyList<FeedImportDecisionEvent> Events => _events.AsReadOnly();

    protected FeedImportDecision() { }

    public static Result<FeedImportDecision> Create(
        Guid userId,
        long minifluxEntryId,
        string entryTitle,
        string entryUrl,
        DateTime? publishedAt)
    {
        if (userId == Guid.Empty ||
            minifluxEntryId <= 0 ||
            string.IsNullOrWhiteSpace(entryUrl) ||
            entryUrl.Length > FeedImportConstants.MaxEntryUrlLength ||
            !Uri.TryCreate(entryUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return FeedImportError.BadRequest;
        }

        // The title is informational only: fall back to the URL and truncate rather than reject the entry.
        var title = string.IsNullOrWhiteSpace(entryTitle) ? entryUrl : entryTitle.Trim();
        if (title.Length > FeedImportConstants.MaxEntryTitleLength)
        {
            title = title[..FeedImportConstants.MaxEntryTitleLength];
        }

        var now = DateTime.UtcNow;
        var decision = new FeedImportDecision
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            MinifluxEntryId = minifluxEntryId,
            EntryTitle = title,
            EntryUrl = entryUrl,
            PublishedAt = publishedAt,
            Status = FeedImportDecisionStatus.Pending,
            Reason = CreatedReason,
            DecidedBy = FeedImportDecidedBy.Auto,
            CreatedAt = now,
            UpdatedAt = now
        };

        decision._events.Add(FeedImportDecisionEvent.Create(
            decision.Id, now, previousStatus: null, FeedImportDecisionStatus.Pending, FeedImportDecidedBy.Auto, CreatedReason));

        return decision;
    }
}
#pragma warning restore CA1054, CA1056

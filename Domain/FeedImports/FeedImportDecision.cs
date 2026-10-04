using System.Text.Json;
using Domain.Primitives;

namespace Domain.FeedImports;

#pragma warning disable CA1054, CA1056 // URL stored as string in database

public class FeedImportDecision : Entity<Guid>
{
    public const string CreatedReason = "Article enregistré depuis Miniflux.";

    public Guid UserId { get; private set; }

    public long MinifluxEntryId { get; private set; }

    // 0 for the decision created from the Miniflux entry; 1..n for the other books found in the same article.
    public int ItemIndex { get; private set; }

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

    public FeedImportArbitrationKind ArbitrationKind { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public FeedImportDecidedBy DecidedBy { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? ErrorStep { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private readonly List<FeedImportDecisionEvent> _events = [];
    public IReadOnlyList<FeedImportDecisionEvent> Events => _events.AsReadOnly();

    private static readonly JsonSerializerOptions s_linksJsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly FeedImportDecisionStatus[] s_analyzableStatuses =
        [FeedImportDecisionStatus.Pending, FeedImportDecisionStatus.AwaitingArbitration];

    private static readonly FeedImportDecisionStatus[] s_finalStatuses =
        [FeedImportDecisionStatus.SkippedDuplicate, FeedImportDecisionStatus.Downloaded, FeedImportDecisionStatus.Imported, FeedImportDecisionStatus.Ignored, FeedImportDecisionStatus.Failed];

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

    public IReadOnlyList<DownloadCandidate> GetCandidates()
    {
        if (string.IsNullOrWhiteSpace(Links))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<DownloadCandidate>>(Links, s_linksJsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    // Another book found in the same article gets its own decision (same entry, next ItemIndex).
    public Result<FeedImportDecision> CreateSibling(int itemIndex)
    {
        if (itemIndex <= 0 || ItemIndex != 0)
        {
            return FeedImportError.BadRequest;
        }

        if (Status != FeedImportDecisionStatus.Pending)
        {
            return FeedImportError.InvalidStatusTransition;
        }

        var now = DateTime.UtcNow;
        var reason = $"Livre {itemIndex + 1} trouvé dans l'article.";
        var sibling = new FeedImportDecision
        {
            Id = Guid.CreateVersion7(),
            UserId = UserId,
            MinifluxEntryId = MinifluxEntryId,
            ItemIndex = itemIndex,
            EntryTitle = EntryTitle,
            EntryUrl = EntryUrl,
            PublishedAt = PublishedAt,
            Status = FeedImportDecisionStatus.Pending,
            Reason = reason,
            DecidedBy = FeedImportDecidedBy.Auto,
            // Same registration date as the article, so the books of an article stay together in the list.
            CreatedAt = CreatedAt,
            UpdatedAt = now
        };
        sibling._events.Add(FeedImportDecisionEvent.Create(
            sibling.Id, now, previousStatus: null, FeedImportDecisionStatus.Pending, FeedImportDecidedBy.Auto, reason));

        return sibling;
    }

    public Result RecordLinks(IReadOnlyList<DownloadCandidate> candidates, ParsedComicTitle parsed, string reason, FeedImportDecidedBy decidedBy)
    {
        var check = EnsureCanAnalyze(candidates, parsed);
        if (check.IsFailure)
        {
            return check;
        }

        SetAnalysis(candidates, parsed);
        MatchedBookId = null;
        ArbitrationKind = FeedImportArbitrationKind.None;
        Transition(FeedImportDecisionStatus.LinksExtracted, reason, decidedBy);
        return Result.Success();
    }

    public Result RequestArbitration(
        FeedImportArbitrationKind kind,
        IReadOnlyList<DownloadCandidate> candidates,
        ParsedComicTitle parsed,
        Guid? matchedBookId,
        string reason,
        FeedImportDecidedBy decidedBy)
    {
        if (kind == FeedImportArbitrationKind.None || !Enum.IsDefined(kind))
        {
            return FeedImportError.BadRequest;
        }

        var check = EnsureCanAnalyze(candidates, parsed);
        if (check.IsFailure)
        {
            return check;
        }

        SetAnalysis(candidates, parsed);
        MatchedBookId = matchedBookId;
        ArbitrationKind = kind;
        Transition(FeedImportDecisionStatus.AwaitingArbitration, reason, decidedBy);
        return Result.Success();
    }

    public Result MarkDuplicate(
        IReadOnlyList<DownloadCandidate> candidates,
        ParsedComicTitle parsed,
        Guid matchedBookId,
        string reason,
        FeedImportDecidedBy decidedBy)
    {
        if (matchedBookId == Guid.Empty)
        {
            return FeedImportError.BadRequest;
        }

        var check = EnsureCanAnalyze(candidates, parsed);
        if (check.IsFailure)
        {
            return check;
        }

        SetAnalysis(candidates, parsed);
        MatchedBookId = matchedBookId;
        ArbitrationKind = FeedImportArbitrationKind.None;
        Transition(FeedImportDecisionStatus.SkippedDuplicate, reason, decidedBy);
        return Result.Success();
    }

    public Result ConfirmNotDuplicate()
    {
        if (Status != FeedImportDecisionStatus.AwaitingArbitration || ArbitrationKind != FeedImportArbitrationKind.ProbableDuplicate)
        {
            return FeedImportError.InvalidStatusTransition;
        }

        MatchedBookId = null;
        ArbitrationKind = FeedImportArbitrationKind.None;
        Transition(FeedImportDecisionStatus.LinksExtracted, "Pas un doublon (confirmé par l'utilisateur).", FeedImportDecidedBy.User);
        return Result.Success();
    }

    public Result ConfirmDuplicate()
    {
        if (Status != FeedImportDecisionStatus.AwaitingArbitration ||
            ArbitrationKind != FeedImportArbitrationKind.ProbableDuplicate ||
            MatchedBookId is null)
        {
            return FeedImportError.InvalidStatusTransition;
        }

        ArbitrationKind = FeedImportArbitrationKind.None;
        Transition(FeedImportDecisionStatus.SkippedDuplicate, "Doublon confirmé par l'utilisateur.", FeedImportDecidedBy.User);
        return Result.Success();
    }

    public Result StartDownload()
    {
        if (Status != FeedImportDecisionStatus.LinksExtracted)
        {
            return FeedImportError.InvalidStatusTransition;
        }

        Transition(FeedImportDecisionStatus.Downloading, "Téléchargement en cours via Debrid-Link.", FeedImportDecidedBy.Auto);
        return Result.Success();
    }

    // A mirror failed but others may still work: traced in the history, the status does not change.
    public Result NoteMirrorFailure(string host, string error)
    {
        if (Status != FeedImportDecisionStatus.Downloading || string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(error))
        {
            return Status == FeedImportDecisionStatus.Downloading ? FeedImportError.BadRequest : FeedImportError.InvalidStatusTransition;
        }

        var now = DateTime.UtcNow;
        UpdatedAt = now;
        _events.Add(FeedImportDecisionEvent.Create(
            Id, now, Status, Status, FeedImportDecidedBy.Auto,
            Truncate($"Échec du miroir {host} : {error}", FeedImportConstants.MaxEventDescriptionLength)));
        return Result.Success();
    }

    public Result MarkDownloaded(string chosenMirror, Guid importJobId, string targetLibraryName)
    {
        if (string.IsNullOrWhiteSpace(chosenMirror) || importJobId == Guid.Empty || string.IsNullOrWhiteSpace(targetLibraryName))
        {
            return FeedImportError.BadRequest;
        }

        if (Status != FeedImportDecisionStatus.Downloading)
        {
            return FeedImportError.InvalidStatusTransition;
        }

        ChosenMirror = Truncate(chosenMirror, FeedImportConstants.MaxChosenMirrorLength);
        ImportJobId = importJobId;
        // Final state for the feed import: the import itself is followed on the Import page.
        Transition(FeedImportDecisionStatus.Downloaded, $"Fichier déposé dans « {targetLibraryName} » : suivi de l'import dans la page Import.", FeedImportDecidedBy.Auto);
        return Result.Success();
    }

    // ── Manual actions ───────────────────────────────────────────────────────

    private static readonly FeedImportDecisionStatus[] s_ignorableStatuses =
    [
        FeedImportDecisionStatus.Pending, FeedImportDecisionStatus.LinksExtracted, FeedImportDecisionStatus.AwaitingArbitration,
        FeedImportDecisionStatus.SkippedDuplicate, FeedImportDecisionStatus.Failed
    ];

    private static readonly FeedImportDecisionStatus[] s_correctableStatuses =
    [
        FeedImportDecisionStatus.LinksExtracted, FeedImportDecisionStatus.AwaitingArbitration,
        FeedImportDecisionStatus.SkippedDuplicate, FeedImportDecisionStatus.Failed, FeedImportDecisionStatus.Ignored
    ];

    public bool CanIgnore => s_ignorableStatuses.Contains(Status);

    // Download despite a (probable) duplicate.
    public bool CanForceDownload =>
        HasSingleCandidate &&
        (Status == FeedImportDecisionStatus.SkippedDuplicate ||
         (Status == FeedImportDecisionStatus.AwaitingArbitration && ArbitrationKind == FeedImportArbitrationKind.ProbableDuplicate));

    // Serie / title / volume describe a single book: not available while the links still have to be grouped.
    public bool CanCorrect => HasSingleCandidate && s_correctableStatuses.Contains(Status);

    public bool CanRetry(DateTime utcNow) =>
        Status is FeedImportDecisionStatus.Failed or FeedImportDecisionStatus.Ignored ||
        (Status == FeedImportDecisionStatus.Downloading && !IsDownloadInProgress(utcNow));

    // Any decision but a running download (an interrupted one can be deleted once stale).
    // The import job of a downloaded decision is checked by the caller.
    public bool CanDelete(DateTime utcNow) => !IsDownloadInProgress(utcNow);

    private bool IsDownloadInProgress(DateTime utcNow) =>
        Status == FeedImportDecisionStatus.Downloading && utcNow - UpdatedAt < FeedImportConstants.StaleDownloadDelay;

    private bool HasSingleCandidate => GetCandidates().Count == 1;

    public Result Ignore()
    {
        if (!CanIgnore)
        {
            return FeedImportError.InvalidStatusTransition;
        }

        ArbitrationKind = FeedImportArbitrationKind.None;
        Transition(FeedImportDecisionStatus.Ignored, "Ignoré par l'utilisateur.", FeedImportDecidedBy.User);
        return Result.Success();
    }

    public Result ForceDownload()
    {
        if (!CanForceDownload)
        {
            return FeedImportError.InvalidStatusTransition;
        }

        MatchedBookId = null;
        ArbitrationKind = FeedImportArbitrationKind.None;
        Transition(FeedImportDecisionStatus.LinksExtracted, "Téléchargement forcé par l'utilisateur (doublon ignoré).", FeedImportDecidedBy.User);
        return Result.Success();
    }

    // Back to Pending, links kept: the caller re-runs the duplicate check (or the analysis when there is no link yet).
    public Result Retry(DateTime utcNow)
    {
        if (!CanRetry(utcNow))
        {
            return FeedImportError.InvalidStatusTransition;
        }

        Reopen("Relancé par l'utilisateur.");
        return Result.Success();
    }

    public Result Correct(ParsedComicTitle parsed)
    {
        if (parsed is null || !parsed.HasSerie)
        {
            return FeedImportError.BadRequest;
        }

        if (!CanCorrect)
        {
            return FeedImportError.InvalidStatusTransition;
        }

        ParsedSerie = TruncateOrNull(parsed.Serie, FeedImportConstants.MaxParsedSerieLength);
        ParsedTitle = TruncateOrNull(parsed.Title, FeedImportConstants.MaxParsedTitleLength);
        ParsedVolume = parsed.Volume;
        var volume = parsed.Volume is { } v ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $" T{v}") : string.Empty;
        Reopen($"Corrigé par l'utilisateur : {ParsedSerie}{volume}{(ParsedTitle is null ? string.Empty : $" · {ParsedTitle}")}.");
        return Result.Success();
    }

    private void Reopen(string reason)
    {
        MatchedBookId = null;
        ArbitrationKind = FeedImportArbitrationKind.None;
        ErrorStep = null;
        ErrorMessage = null;
        Transition(FeedImportDecisionStatus.Pending, reason, FeedImportDecidedBy.User);
    }

    public Result Fail(string step, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(step) || string.IsNullOrWhiteSpace(errorMessage))
        {
            return FeedImportError.BadRequest;
        }

        if (s_finalStatuses.Contains(Status))
        {
            return FeedImportError.InvalidStatusTransition;
        }

        ErrorStep = Truncate(step, FeedImportConstants.MaxErrorStepLength);
        ErrorMessage = Truncate(errorMessage, FeedImportConstants.MaxErrorMessageLength);
        ArbitrationKind = FeedImportArbitrationKind.None;
        Transition(FeedImportDecisionStatus.Failed, errorMessage, FeedImportDecidedBy.Auto);
        return Result.Success();
    }

    private Result EnsureCanAnalyze(IReadOnlyList<DownloadCandidate> candidates, ParsedComicTitle parsed)
    {
        if (candidates is null || candidates.Count == 0 || candidates.Any(c => c.Mirrors.Count == 0) || parsed is null)
        {
            return FeedImportError.BadRequest;
        }

        return s_analyzableStatuses.Contains(Status) ? Result.Success() : FeedImportError.InvalidStatusTransition;
    }

    private void SetAnalysis(IReadOnlyList<DownloadCandidate> candidates, ParsedComicTitle parsed)
    {
        Links = JsonSerializer.Serialize(candidates, s_linksJsonOptions);
        ParsedSerie = TruncateOrNull(parsed.Serie, FeedImportConstants.MaxParsedSerieLength);
        ParsedTitle = TruncateOrNull(parsed.Title, FeedImportConstants.MaxParsedTitleLength);
        ParsedVolume = parsed.Volume;
    }

    private void Transition(FeedImportDecisionStatus newStatus, string reason, FeedImportDecidedBy decidedBy)
    {
        var previousStatus = Status;
        var now = DateTime.UtcNow;
        Status = newStatus;
        Reason = Truncate(reason, FeedImportConstants.MaxReasonLength);
        DecidedBy = decidedBy;
        UpdatedAt = now;
        _events.Add(FeedImportDecisionEvent.Create(Id, now, previousStatus, newStatus, decidedBy, Reason));
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;

    private static string? TruncateOrNull(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : Truncate(value.Trim(), maxLength);
}
#pragma warning restore CA1054, CA1056

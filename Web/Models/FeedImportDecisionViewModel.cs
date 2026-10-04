using System.Globalization;
using Domain.FeedImports;
using MudBlazor;

namespace Web.Models;

public sealed record FeedImportDecisionEventViewModel(
    DateTime OccurredAt,
    string OccurredAtDisplay,
    string StatusDisplay,
    string DecidedByDisplay,
    string Description);

#pragma warning disable CA1054, CA1056 // URL displayed as a link, validated by the domain
public sealed record FeedImportMirrorViewModel(string Url, string Host, bool IsChosen = false);

public sealed record FeedImportCandidateViewModel(int Index, string Label, string? FileName, string? SizeDisplay, IReadOnlyList<FeedImportMirrorViewModel> Mirrors)
{
    public string DisplayName => FileName ?? Label;
}

// Manual actions available on a decision (computed by the domain), with the values to prefill the correction form.
public sealed record FeedImportDecisionActions(
    bool CanForceDownload,
    bool CanRetry,
    bool CanCorrect,
    bool CanIgnore,
    string? Serie,
    string? Title,
    int? Volume)
{
    public static FeedImportDecisionActions None { get; } = new(false, false, false, false, null, null, null);

    public bool Any => CanForceDownload || CanRetry || CanCorrect || CanIgnore;
}

public sealed record FeedImportDecisionViewModel(
    Guid Id,
    string EntryTitle,
    string EntryUrl,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    FeedImportDecisionStatus Status,
    string StatusDisplay,
    Color StatusColor,
    string Reason,
    string DecidedByDisplay,
    string? ParsedDisplay,
    string? ErrorDisplay,
    IReadOnlyList<FeedImportDecisionEventViewModel> Events,
    string? ItemDisplay,
    FeedImportArbitrationKind ArbitrationKind,
    Guid? MatchedBookId,
    IReadOnlyList<FeedImportCandidateViewModel> Candidates,
    string? ImportPageUrl = null,
    FeedImportDecisionActions? Actions = null,
    bool CanDelete = false)
#pragma warning restore CA1054, CA1056
{
    private static readonly CultureInfo s_displayCulture = CultureInfo.GetCultureInfo("fr-FR");

    public string CreatedAtDisplay => FormatDate(CreatedAt);

    public FeedImportDecisionActions ManualActions => Actions ?? FeedImportDecisionActions.None;

    public string PublishedAtDisplay => PublishedAt.HasValue ? FormatDate(PublishedAt.Value) : "-";

    public bool CanChooseCandidate =>
        Status == FeedImportDecisionStatus.AwaitingArbitration && ArbitrationKind == FeedImportArbitrationKind.AmbiguousLinks;

    public bool CanResolveDuplicate =>
        Status == FeedImportDecisionStatus.AwaitingArbitration && ArbitrationKind == FeedImportArbitrationKind.ProbableDuplicate;

    // Imported is never set: once downloaded, the import is followed on the Import page.
    public static IReadOnlyList<FeedImportDecisionStatus> FilterableStatuses { get; } =
        Enum.GetValues<FeedImportDecisionStatus>().Where(s => s != FeedImportDecisionStatus.Imported).ToList();

    // isPartOfMultiBookArticle: the article was split into several books, so the first one is labelled too.
    // importLibraryId: library the downloads are deposited in, preselected on the Import page.
    public static FeedImportDecisionViewModel From(FeedImportDecision decision, bool isPartOfMultiBookArticle = false, Guid? importLibraryId = null)
    {
        ArgumentNullException.ThrowIfNull(decision);

        return new(
            Id: decision.Id,
            EntryTitle: decision.EntryTitle,
            EntryUrl: decision.EntryUrl,
            PublishedAt: decision.PublishedAt,
            CreatedAt: decision.CreatedAt,
            Status: decision.Status,
            StatusDisplay: GetStatusDisplay(decision.Status),
            StatusColor: GetStatusColor(decision.Status),
            Reason: decision.Reason,
            DecidedByDisplay: GetDecidedByDisplay(decision.DecidedBy),
            ParsedDisplay: GetParsedDisplay(decision.ParsedSerie, decision.ParsedTitle, decision.ParsedVolume),
            ErrorDisplay: GetErrorDisplay(decision.ErrorStep, decision.ErrorMessage),
            Events: decision.Events
                .OrderBy(e => e.OccurredAt)
                .ThenBy(e => e.Id)
                .Select(e => new FeedImportDecisionEventViewModel(
                    e.OccurredAt, FormatDate(e.OccurredAt), GetStatusDisplay(e.Status), GetDecidedByDisplay(e.DecidedBy), e.Description))
                .ToList(),
            ItemDisplay: isPartOfMultiBookArticle || decision.ItemIndex > 0
                ? string.Create(CultureInfo.InvariantCulture, $"Livre {decision.ItemIndex + 1} de l'article")
                : null,
            ArbitrationKind: decision.ArbitrationKind,
            MatchedBookId: decision.MatchedBookId,
            Candidates: decision.GetCandidates()
                .Select((c, i) => new FeedImportCandidateViewModel(
                    i,
                    c.Label,
                    c.FileName,
                    c.SizeBytes.HasValue ? FormatSize(c.SizeBytes.Value) : null,
                    c.Mirrors.Select(m => new FeedImportMirrorViewModel(m.Url, m.Host, m.Url == decision.ChosenMirror)).ToList()))
                .ToList(),
            ImportPageUrl: GetImportPageUrl(decision.Status, importLibraryId),
            Actions: new FeedImportDecisionActions(
                decision.CanForceDownload,
                decision.CanRetry(DateTime.UtcNow),
                decision.CanCorrect,
                decision.CanIgnore,
                decision.ParsedSerie,
                decision.ParsedTitle,
                decision.ParsedVolume),
            CanDelete: decision.CanDelete(DateTime.UtcNow));
    }

    private static string? GetImportPageUrl(FeedImportDecisionStatus status, Guid? importLibraryId)
    {
        if (status != FeedImportDecisionStatus.Downloaded)
        {
            return null;
        }

        return importLibraryId is { } libraryId
            ? string.Create(CultureInfo.InvariantCulture, $"/import?libraryId={libraryId}")
            : "/import";
    }

    // Fixed French format: "03/10/2026 19:23" whatever the server culture.
    private static string FormatDate(DateTime utc) =>
        utc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", s_displayCulture);

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => string.Create(CultureInfo.InvariantCulture, $"{bytes / 1_073_741_824.0:F1} Go"),
        >= 1_048_576 => string.Create(CultureInfo.InvariantCulture, $"{bytes / 1_048_576.0:F1} Mo"),
        >= 1_024 => string.Create(CultureInfo.InvariantCulture, $"{bytes / 1_024.0:F0} Ko"),
        _ => string.Create(CultureInfo.InvariantCulture, $"{bytes} o")
    };

    public static string GetStatusDisplay(FeedImportDecisionStatus status) => status switch
    {
        FeedImportDecisionStatus.Pending => "En attente",
        FeedImportDecisionStatus.LinksExtracted => "Liens extraits",
        FeedImportDecisionStatus.AwaitingArbitration => "À arbitrer",
        FeedImportDecisionStatus.SkippedDuplicate => "Doublon",
        FeedImportDecisionStatus.Downloading => "Téléchargement...",
        FeedImportDecisionStatus.Downloaded => "Téléchargé",
        FeedImportDecisionStatus.Imported => "Importé",
        FeedImportDecisionStatus.Ignored => "Ignoré",
        FeedImportDecisionStatus.Failed => "Échoué",
        _ => status.ToString()
    };

    private static Color GetStatusColor(FeedImportDecisionStatus status) => status switch
    {
        FeedImportDecisionStatus.AwaitingArbitration => Color.Warning,
        FeedImportDecisionStatus.LinksExtracted or FeedImportDecisionStatus.Downloading => Color.Info,
        FeedImportDecisionStatus.Downloaded or FeedImportDecisionStatus.Imported => Color.Success,
        FeedImportDecisionStatus.Failed => Color.Error,
        _ => Color.Default
    };

    private static string GetDecidedByDisplay(FeedImportDecidedBy decidedBy) => decidedBy switch
    {
        FeedImportDecidedBy.User => "Utilisateur",
        _ => "Automatique"
    };

    private static string? GetParsedDisplay(string? serie, string? title, int? volume)
    {
        var parts = new List<string>(3);
        if (!string.IsNullOrWhiteSpace(serie))
        {
            parts.Add(serie);
        }
        if (volume.HasValue)
        {
            parts.Add(string.Create(CultureInfo.InvariantCulture, $"T{volume.Value}"));
        }
        if (!string.IsNullOrWhiteSpace(title))
        {
            parts.Add(title);
        }
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static string? GetErrorDisplay(string? step, string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }
        return string.IsNullOrWhiteSpace(step) ? message : $"{step} : {message}";
    }
}

using System.Globalization;
using Domain.FeedImports;
using MudBlazor;

namespace Web.Models;

public sealed record FeedImportDecisionEventViewModel(
    DateTime OccurredAt,
    string StatusDisplay,
    string DecidedByDisplay,
    string Description);

#pragma warning disable CA1054, CA1056 // URL displayed as a link, validated by the domain
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
    IReadOnlyList<FeedImportDecisionEventViewModel> Events)
#pragma warning restore CA1054, CA1056
{
    public static IReadOnlyList<FeedImportDecisionStatus> FilterableStatuses { get; } = Enum.GetValues<FeedImportDecisionStatus>();

    public static FeedImportDecisionViewModel From(FeedImportDecision decision)
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
                    e.OccurredAt, GetStatusDisplay(e.Status), GetDecidedByDisplay(e.DecidedBy), e.Description))
                .ToList());
    }

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
        FeedImportDecisionStatus.LinksExtracted or FeedImportDecisionStatus.Downloading or FeedImportDecisionStatus.Downloaded => Color.Info,
        FeedImportDecisionStatus.Imported => Color.Success,
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

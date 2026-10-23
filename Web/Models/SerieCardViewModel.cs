using Application.Series;
using Domain.Series;

namespace Web.Models;

public enum SerieBadgeKind
{
    None,
    Complete,
    Missing
}

// A series of a library as its card shows it (#1205).
public sealed record SerieCardViewModel(
    string Name,
    string Cover,
    string Href,
    string CountLabel,
    string Detail,
    string? Badge,
    SerieBadgeKind BadgeKind,
    SerieCompletenessStatus Status)
{
    public static SerieCardViewModel From(SerieSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var completeness = summary.Completeness;
        var volumes = SerieDisplay.VolumesLabel(summary.FirstVolume, summary.LastVolume);
        var (countLabel, detail, badge, badgeKind) = completeness.Status switch
        {
            SerieCompletenessStatus.NotApplicable => (LibraryDisplay.BookCount(summary.BookCount), "Single volumes", null, SerieBadgeKind.None),
            SerieCompletenessStatus.Complete => (VolumesShort(summary.BookCount), $"{volumes} · complete", "Complete", SerieBadgeKind.Complete),
            SerieCompletenessStatus.Incomplete => (VolumesShort(summary.BookCount), $"{volumes} · {SerieDisplay.MissingLabel(completeness.MissingCount)}",
                SerieDisplay.MissingLabel(completeness.MissingCount), SerieBadgeKind.Missing),
            _ => (VolumesShort(summary.BookCount), volumes, (string?)null, SerieBadgeKind.None),
        };

        return new SerieCardViewModel(
            summary.Name,
            summary.Cover,
            SerieDisplay.SerieHref(summary.LibraryId, summary.Name),
            countLabel,
            detail,
            badge,
            badgeKind,
            completeness.Status);
    }

    public bool Matches(SeriesFilter filter) => filter switch
    {
        SeriesFilter.Incomplete => Status == SerieCompletenessStatus.Incomplete,
        SeriesFilter.Complete => Status == SerieCompletenessStatus.Complete,
        _ => true,
    };

    private static string VolumesShort(int count) => count == 1 ? "1 vol" : $"{count} vols";
}

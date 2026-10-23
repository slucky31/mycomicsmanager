using Application.Series.Detail;
using Domain.Series;

namespace Web.Models;

// The page of a series (#1205): how complete it is and its volumes.
public sealed record SeriePageViewModel(
    string Name,
    string Subtitle,
    bool ShowsCompleteness,
    string ProgressLabel,
    string ProgressCount,
    int ProgressPercent,
    string PublishedLabel,
    int? TotalVolumes,
    bool IsFinished,
    IReadOnlyList<SerieSlotViewModel> Slots)
{
    public static SeriePageViewModel From(SerieDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);

        var summary = detail.Summary;
        var completeness = summary.Completeness;
        var showsCompleteness = completeness.Status != SerieCompletenessStatus.NotApplicable;
        var subtitle = showsCompleteness
            ? $"in {detail.LibraryName} · {completeness.OwnedCount} of {SerieDisplay.VolumeCount(completeness.ExpectedCount)}"
            : $"in {detail.LibraryName} · {LibraryDisplay.BookCount(summary.BookCount)}";

        return new SeriePageViewModel(
            summary.Name,
            subtitle,
            showsCompleteness,
            ProgressText(completeness),
            $"{completeness.OwnedCount} / {completeness.ExpectedCount}",
            completeness.ExpectedCount == 0 ? 0 : completeness.OwnedCount * 100 / completeness.ExpectedCount,
            PublishedText(summary.TotalVolumes, summary.IsFinished),
            summary.TotalVolumes,
            summary.IsFinished,
            SerieSlotViewModel.Of(detail.Books, completeness.Missing));
    }

    private static string ProgressText(SerieCompleteness completeness) => completeness.Status switch
    {
        SerieCompletenessStatus.Complete => "Complete",
        SerieCompletenessStatus.Incomplete => completeness.MissingCount == 1 ? "1 volume missing" : $"{completeness.MissingCount} volumes missing",
        _ => "No gap, number of volumes published unknown",
    };

    private static string PublishedText(int? totalVolumes, bool isFinished)
    {
        if (totalVolumes is not { } total)
        {
            return "Volumes published: unknown";
        }

        var state = isFinished ? "finished" : "ongoing";
        return $"Volumes published: {total} · {state}";
    }
}

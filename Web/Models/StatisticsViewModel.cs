using System.Globalization;
using Application.Statistics.Get;
using MudBlazor;

namespace Web.Models;

public sealed record StatisticTileViewModel(string Label, string Value, string Icon, string? Suffix = null);

public sealed record StatisticsViewModel(
    bool HasBooks,
    string TotalBooks,
    string BooksUnit,
    string ReadBooks,
    string UnreadBooks,
    int ReadPercent,
    IReadOnlyList<StatisticTileViewModel> Tiles,
    ReadingsChartViewModel Chart)
{
    public static StatisticsViewModel From(StatisticsDto dto, bool allLibraries)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // Rounded down so that a single unread book never shows "100%".
        var readPercent = dto.TotalBooks > 0 ? dto.ReadBooks * 100 / dto.TotalBooks : 0;

        return new StatisticsViewModel(
            dto.TotalBooks > 0,
            FormatNumber(dto.TotalBooks),
            dto.TotalBooks == 1 ? "book" : "books",
            FormatNumber(dto.ReadBooks),
            FormatNumber(dto.UnreadBooks),
            readPercent,
            BuildTiles(dto, allLibraries),
            ReadingsChartViewModel.From(dto.ReadingsPerMonth));
    }

    private static List<StatisticTileViewModel> BuildTiles(StatisticsDto dto, bool allLibraries)
    {
        var tiles = new List<StatisticTileViewModel>
        {
            new("Series", FormatNumber(dto.TotalSeries), Icons.Material.Outlined.CollectionsBookmark),
            new("Readings", FormatNumber(dto.TotalReadings), Icons.Material.Outlined.AutoStories),
            FormatRating(dto.AverageRating),
            new("Pages", FormatNumber(dto.TotalPages), Icons.Material.Outlined.Description)
        };

        if (allLibraries)
        {
            tiles.Add(new("Libraries", FormatNumber(dto.TotalLibraries), Icons.Material.Outlined.LocalLibrary));
        }

        if (dto.DigitalStorageBytes > 0)
        {
            tiles.Add(new("Storage", FileSizeFormatter.Format(dto.DigitalStorageBytes), Icons.Material.Outlined.Storage));
        }

        return tiles;
    }

    internal static string FormatNumber(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private static StatisticTileViewModel FormatRating(double? rating) => rating is { } value
        ? new("Avg rating", value.ToString("0.0", CultureInfo.InvariantCulture), Icons.Material.Outlined.StarOutline, " / 5")
        : new("Avg rating", "–", Icons.Material.Outlined.StarOutline);
}

public sealed record ReadingsChartBarViewModel(string Initial, string Month, int Count, double HeightPercent, bool IsPeak);

/// <summary>
/// The readings of the last months as bars, scaled on an axis of two equal steps (0, step, 2 × step).
/// </summary>
public sealed record ReadingsChartViewModel(
    IReadOnlyList<ReadingsChartBarViewModel> Bars,
    IReadOnlyList<int> AxisTicks,
    int Total,
    string AriaLabel)
{
    private static readonly int[] s_stepMultipliers = [1, 2, 5];

    public static ReadingsChartViewModel From(IReadOnlyList<MonthlyReadingCountDto> months)
    {
        ArgumentNullException.ThrowIfNull(months);

        var total = months.Sum(m => m.Count);
        var peak = months.Count > 0 ? months.Max(m => m.Count) : 0;
        // The most recent month wins when several share the peak.
        var peakIndex = peak > 0 ? LastIndexOf(months, peak) : -1;
        var step = AxisStep(peak);
        var axisMax = 2 * step;

        var bars = months
            .Select((m, index) =>
            {
                var date = new DateTime(m.Year, m.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                return new ReadingsChartBarViewModel(
                    date.ToString("MMMM", CultureInfo.InvariantCulture)[..1],
                    date.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                    m.Count,
                    m.Count * 100d / axisMax,
                    index == peakIndex);
            })
            .ToList();

        return new ReadingsChartViewModel(bars, [0, step, axisMax], total, AriaLabelFor(months, total, peakIndex));
    }

    /// <summary>The smallest step of 1, 2 or 5 × 10ⁿ such that two steps reach the peak.</summary>
    internal static int AxisStep(int peak)
    {
        // The smallest power of ten for which two steps of 5 × 10ⁿ reach the peak.
        var magnitude = 1;
        while (10 * magnitude < peak)
        {
            magnitude *= 10;
        }

        return s_stepMultipliers.Select(multiplier => multiplier * magnitude).First(step => 2 * step >= peak);
    }

    private static int LastIndexOf(IReadOnlyList<MonthlyReadingCountDto> months, int count)
    {
        for (var index = months.Count - 1; index > 0; index--)
        {
            if (months[index].Count == count)
            {
                return index;
            }
        }

        return 0;
    }

    private static string AriaLabelFor(IReadOnlyList<MonthlyReadingCountDto> months, int total, int peakIndex)
    {
        var period = $"Readings per month over the last {months.Count} months";
        if (peakIndex < 0)
        {
            return $"{period}: no readings";
        }

        var peak = months[peakIndex];
        var peakMonth = new DateTime(peak.Year, peak.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .ToString("MMMM", CultureInfo.InvariantCulture);
        var readings = total == 1 ? "reading" : "readings";
        return $"{period}: {StatisticsViewModel.FormatNumber(total)} {readings}, peak of {peak.Count} in {peakMonth}";
    }
}

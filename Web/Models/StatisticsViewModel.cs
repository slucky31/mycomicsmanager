using System.Globalization;
using Application.Statistics.Get;
using MudBlazor;

namespace Web.Models;

public sealed record StatisticCardViewModel(string Label, string Value, string Icon);

public sealed record StatisticsViewModel(
    bool HasBooks,
    IReadOnlyList<StatisticCardViewModel> KeyFigures,
    List<ChartSeries<double>> ReadingsSeries,
    IReadOnlyList<string> MonthLabels,
    int ReadingsInPeriod)
{
    public const string ReadingsSeriesName = "Readings";

    private static readonly string[] s_sizeUnits = ["B", "KB", "MB", "GB", "TB"];

    public static StatisticsViewModel From(StatisticsDto dto, bool allLibraries)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var series = new List<ChartSeries<double>>
        {
            new()
            {
                Name = ReadingsSeriesName,
                Data = dto.ReadingsPerMonth.Select(m => (double)m.Count).ToArray()
            }
        };

        var labels = dto.ReadingsPerMonth
            .Select(m => new DateTime(m.Year, m.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                .ToString("MMM yy", CultureInfo.InvariantCulture))
            .ToArray();

        return new StatisticsViewModel(
            dto.TotalBooks > 0,
            BuildKeyFigures(dto, allLibraries),
            series,
            labels,
            dto.ReadingsPerMonth.Sum(m => m.Count));
    }

    private static List<StatisticCardViewModel> BuildKeyFigures(StatisticsDto dto, bool allLibraries)
    {
        var cards = new List<StatisticCardViewModel>
        {
            new("Books", FormatNumber(dto.TotalBooks), Icons.Material.Filled.MenuBook),
            new("Series", FormatNumber(dto.TotalSeries), Icons.Material.Filled.CollectionsBookmark)
        };

        if (allLibraries)
        {
            cards.Add(new("Libraries", FormatNumber(dto.TotalLibraries), Icons.Material.Filled.LocalLibrary));
        }

        cards.Add(new("Read", FormatNumber(dto.ReadBooks), Icons.Material.Filled.CheckCircle));
        cards.Add(new("Unread", FormatNumber(dto.UnreadBooks), Icons.Material.Filled.HourglassEmpty));
        cards.Add(new("Readings", FormatNumber(dto.TotalReadings), Icons.Material.Filled.AutoStories));
        cards.Add(new("Average rating", FormatRating(dto.AverageRating), Icons.Material.Filled.Star));
        cards.Add(new("Pages", FormatNumber(dto.TotalPages), Icons.Material.Filled.Description));

        if (dto.DigitalStorageBytes > 0)
        {
            cards.Add(new("Digital storage", FormatBytes(dto.DigitalStorageBytes), Icons.Material.Filled.Storage));
        }

        return cards;
    }

    private static string FormatNumber(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private static string FormatRating(double? rating) =>
        rating is { } value ? $"{value.ToString("0.0", CultureInfo.InvariantCulture)} / 5" : "–";

    public static string FormatBytes(long bytes)
    {
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < s_sizeUnits.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        var format = unit == 0 ? "0" : "0.#";
        return $"{size.ToString(format, CultureInfo.InvariantCulture)} {s_sizeUnits[unit]}";
    }
}

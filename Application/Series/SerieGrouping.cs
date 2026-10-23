using System.Globalization;
using Domain.Series;

namespace Application.Series;

// Groups the books of the libraries into their series and tells what each one is missing (#1205).
public static class SerieGrouping
{
    private static readonly StringComparer s_nameComparer = StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true);

    // The series of the books, by name then library; each book row belongs to exactly one.
    public static IReadOnlyList<SerieSummary> Summarize(IEnumerable<SerieBookRow> books, IEnumerable<SeriesInfo> infos)
    {
        ArgumentNullException.ThrowIfNull(books);
        ArgumentNullException.ThrowIfNull(infos);

        var infosByKey = infos
            .GroupBy(i => (i.LibraryId, i.NormalizedName))
            .ToDictionary(g => g.Key, g => g.First());

        return books
            .GroupBy(b => (b.LibraryId, NormalizedName: SerieNames.Normalize(b.Serie)))
            .Select(g => Summarize(OrderByVolume(g), infosByKey.GetValueOrDefault(g.Key)))
            .OrderBy(s => s.Name, s_nameComparer)
            .ThenBy(s => s.LibraryId)
            .ToList();
    }

    public static SerieSummary Summarize(IReadOnlyList<SerieBookRow> orderedBooks, SeriesInfo? info)
    {
        ArgumentNullException.ThrowIfNull(orderedBooks);
        if (orderedBooks.Count == 0)
        {
            throw new ArgumentException("A series has at least one book.", nameof(orderedBooks));
        }

        var first = orderedBooks[0];
        var completeness = SerieNames.IsOneShot(first.Serie)
            ? SerieCompleteness.NotApplicable
            : SerieCompleteness.Of(orderedBooks.Select(b => b.VolumeNumber), info?.TotalVolumes);

        return new SerieSummary(
            first.LibraryId,
            first.Serie.Trim(),
            orderedBooks.Count,
            orderedBooks.FirstOrDefault(b => !string.IsNullOrEmpty(b.ImageLink))?.ImageLink ?? string.Empty,
            first.VolumeNumber,
            orderedBooks[^1].VolumeNumber,
            info?.TotalVolumes,
            info?.IsFinished ?? false,
            completeness);
    }

    // The books of one series, by volume then id so the order is stable.
    public static IReadOnlyList<SerieBookRow> OrderByVolume(IEnumerable<SerieBookRow> books) =>
        books.OrderBy(b => b.VolumeNumber).ThenBy(b => b.BookId).ToList();
}

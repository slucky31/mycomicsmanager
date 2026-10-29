using System.Globalization;
using Application.Books.Reading;
using Application.Helpers;

namespace Web.Models;

// A book in progress on the Reading page (#1252): its cover opens the reader at the page where it stopped.
public sealed record BookInProgressViewModel(Guid Id, string Title, string SerieAndVolume, string ImageLink, string Progress, int Percent)
{
    public string ReadHref => ReturnLinks.Read(Id, ReturnLinks.ReadingHref);

    public string ProgressStyle => $"width: {Percent.ToString(CultureInfo.InvariantCulture)}%;";

    public string AriaLabel => $"Continue {Title}, {Progress}";

    public static BookInProgressViewModel From(BookInProgressDto book)
    {
        ArgumentNullException.ThrowIfNull(book);

        var (progress, percent) = ReadingProgressDisplay.Of(book.LastReadPage, book.NumberOfPages);
        return new(book.Id, book.Title, BookText.SerieAndVolume(book.Serie, book.Title, book.VolumeNumber) ?? string.Empty, book.ImageLink, progress, percent ?? 0);
    }
}

// A reading date of a book on the Reading page (#1252).
public sealed record BookReadingViewModel(Guid ReadingDateId, Guid BookId, string Title, string SerieAndVolume, string ImageLink, DateTime Date, int Rating)
{
    public string Href => ReturnLinks.Book(BookId, ReturnLinks.ReadingHref);

    public string DateLabel => Date.ToString(PublishDateHelper.DisplayFormat, CultureInfo.InvariantCulture);

    public string RatingLabel => BookDetailViewModel.RatingLabel(Rating);

    public static BookReadingViewModel From(BookReadingDto reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return new(reading.ReadingDateId, reading.BookId, reading.Title, BookText.SerieAndVolume(reading.Serie, reading.Title, reading.VolumeNumber) ?? string.Empty,
            reading.ImageLink, reading.Date, reading.Rating);
    }
}

// The readings of one month, the last one first: "October 2026".
public sealed record ReadingMonthViewModel(string Label, IReadOnlyList<BookReadingViewModel> Readings);

public static class ReadingPageDisplay
{
    public static string Subtitle(int inProgressCount, int readingCount) =>
        $"{inProgressCount} in progress · {(readingCount == 1 ? "1 reading" : $"{readingCount} readings")}";

    public static string MoreLabel(int remaining) => remaining == 1 ? "Show 1 more reading" : $"Show {remaining} more readings";

    // The readings come the last one first: the months keep that order.
    public static IReadOnlyList<ReadingMonthViewModel> ByMonth(IEnumerable<BookReadingViewModel> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);

        return [.. readings
            .GroupBy(r => new DateOnly(r.Date.Year, r.Date.Month, 1))
            .Select(g => new ReadingMonthViewModel(g.Key.ToString("MMMM yyyy", CultureInfo.InvariantCulture), [.. g]))];
    }
}

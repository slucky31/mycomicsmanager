using System.Globalization;
using Application.Helpers;
using Domain.Books;

namespace Web.Models;

// One line of the "Details" grid of the book page.
public sealed record BookDetailField(string Label, string Value);

// One reading of the "Reading history" of the book page.
public sealed record BookReadingEntry(string Date, int Rating)
{
    public string RatingLabel { get; } = BookDetailViewModel.RatingLabel(Rating);
}

// What the book page shows, computed once from the book (ADR-0021).
public sealed record BookDetailViewModel
{
    public required Guid Id { get; init; }
    public required Guid LibraryId { get; init; }
    public required string Title { get; init; }
    public required string ImageLink { get; init; }
    public required string Authors { get; init; }
    public required bool IsDigital { get; init; }

    // "Blacksad · Volume 4"; the series is left out when it is the title.
    public string? SerieAndVolume { get; init; }

    // "Digital · 188 pages" under the authors; nothing for a paper book.
    public string? FormatLabel { get; init; }

    // "Volume 1 · Paper" under the title in the actions sheet.
    public required string ActionsDetails { get; init; }

    // Rating of the most recent reading, 0 when the book was never rated.
    public int LatestRating { get; init; }
    public bool HasBeenRead { get; init; }

    // Main action of a digital book: "Continue reading" and "Page 68 of 188", or "Read".
    public required string ReadLabel { get; init; }
    public string? ReadProgressLabel { get; init; }

    // Width of the progress bar, null when the progress is unknown.
    public int? ReadProgressPercent { get; init; }

    // A started digital book can go back to its first page.
    public bool CanStartOver => ReadProgressLabel is not null;

    public string? Isbn { get; init; }
    public IsbnScanState IsbnScanState { get; init; } = IsbnScanState.HasIsbn;
    public IReadOnlyList<string> IsbnCandidates { get; init; } = [];

    // Shown while a digital book has no ISBN and a single one was not found on its pages.
    public string? IsbnScanNotice { get; init; }
    public string ScanIsbnLabel { get; init; } = "Scan the pages";

    public IReadOnlyList<BookDetailField> Details { get; init; } = [];
    public IReadOnlyList<BookReadingEntry> Readings { get; init; } = [];

    public string LatestRatingLabel => RatingLabel(LatestRating);
    public string CoverAlt => $"Cover of {Title}";
    public string ActionsLabel => $"Actions for {Title}";
    public string LibraryHref => $"/libraries/{LibraryId}";
    public string EditHref => $"/books/{Id}/edit";
    public string FindDetailsHref => $"/books/{Id}/find-details";
    public string ReadHref => $"/books/{Id}/read";
    public string DownloadHref => $"/api/books/{Id}/download";

    public static string RatingLabel(int rating) => $"Rated {rating} out of 5";

    public static BookDetailViewModel From(Book book)
    {
        ArgumentNullException.ThrowIfNull(book);

        var digitalBook = book as DigitalBook;
        var latestReading = book.ReadingDates.MaxBy(rd => rd.Date);
        var volume = BookText.VolumeLabel(book.VolumeNumber);
        var (readLabel, progressLabel, progressPercent) = ReadingProgress(digitalBook);
        var isbnScanState = digitalBook?.GetIsbnScanState() ?? IsbnScanState.HasIsbn;

        return new BookDetailViewModel
        {
            Id = book.Id,
            LibraryId = book.LibraryId,
            Title = book.Title,
            ImageLink = book.ImageLink,
            Authors = book.Authors,
            IsDigital = digitalBook is not null,
            SerieAndVolume = BookText.SerieAndVolume(book.Serie, book.Title, book.VolumeNumber),
            FormatLabel = digitalBook is null ? null : BookText.Join("Digital", BookText.PagesLabel(book.NumberOfPages)),
            ActionsDetails = BookText.Join(volume, digitalBook is null ? "Paper" : "Digital")!,
            LatestRating = latestReading?.Rating ?? 0,
            HasBeenRead = latestReading is not null,
            ReadLabel = readLabel,
            ReadProgressLabel = progressLabel,
            ReadProgressPercent = progressPercent,
            Isbn = string.IsNullOrWhiteSpace(book.ISBN) ? null : book.ISBN,
            IsbnScanState = isbnScanState,
            IsbnCandidates = digitalBook?.IsbnCandidates ?? [],
            IsbnScanNotice = IsbnScanNoticeOf(isbnScanState, digitalBook?.IsbnScannedAt),
            ScanIsbnLabel = isbnScanState == IsbnScanState.NotFound ? "Scan again" : "Scan the pages",
            Details = DetailsOf(book, digitalBook),
            Readings = [.. book.ReadingDates
                .OrderByDescending(rd => rd.Date)
                .Select(rd => new BookReadingEntry(rd.Date.ToString(PublishDateHelper.DisplayFormat, CultureInfo.InvariantCulture), rd.Rating))],
        };
    }

    // The reader records the last page shown (zero-based): "Page 68 of 188" from the second page on.
    private static (string Label, string? Progress, int? Percent) ReadingProgress(DigitalBook? book)
    {
        if (book is null || book.LastReadPage <= 0)
        {
            return ("Read", null, null);
        }

        var page = book.LastReadPage + 1;
        if (book.NumberOfPages is not > 0)
        {
            return ("Continue reading", $"Page {page}", null);
        }

        var pageCount = book.NumberOfPages.Value;
        page = Math.Min(page, pageCount);
        return ("Continue reading", $"Page {page} of {pageCount}", page * 100 / pageCount);
    }

    private static string? IsbnScanNoticeOf(IsbnScanState state, DateTime? scannedAt) => state switch
    {
        IsbnScanState.NotScanned => "The pages were not scanned for an ISBN yet.",
        IsbnScanState.NotFound => $"No ISBN found in the pages (scanned on {scannedAt?.ToString(PublishDateHelper.DisplayFormat, CultureInfo.InvariantCulture)}).",
        _ => null,
    };

    // The fields the book has, in the order of the mockups; the ISBN is shown apart with its icon.
    private static List<BookDetailField> DetailsOf(Book book, DigitalBook? digitalBook)
    {
        var fields = new List<BookDetailField>();
        AddIfAny(fields, "Volume", book.VolumeNumber > 0 ? book.VolumeNumber.ToString(CultureInfo.InvariantCulture) : null);
        AddIfAny(fields, "Publisher", book.Publishers);
        AddIfAny(fields, "Published", book.PublishDate?.ToString(PublishDateHelper.DisplayFormat, CultureInfo.InvariantCulture));
        AddIfAny(fields, "Pages", book.NumberOfPages > 0 ? book.NumberOfPages.Value.ToString(CultureInfo.InvariantCulture) : null);
        // Books imported before their size was recorded have none (0): nothing is shown for them.
        AddIfAny(fields, "File size", digitalBook is { FileSize: > 0 } ? FileSizeFormatter.Format(digitalBook.FileSize) : null);
        AddIfAny(fields, "Added", book.CreatedOnUtc == default ? null : book.CreatedOnUtc.ToString(PublishDateHelper.DisplayFormat, CultureInfo.InvariantCulture));
        return fields;
    }

    private static void AddIfAny(List<BookDetailField> fields, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            fields.Add(new BookDetailField(label, value));
        }
    }
}

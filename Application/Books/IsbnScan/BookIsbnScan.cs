using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;

namespace Application.Books.IsbnScan;

internal static class BookIsbnScan
{
    /// <summary>
    /// Reads the pages of the book and saves what they show. A single ISBN unused by another book
    /// is assigned (the other metadata are left untouched); otherwise the ISBNs are kept as candidates.
    /// </summary>
    public static async Task<Result<IsbnScanOutcome>> ScanAndSaveAsync(
        DigitalBook book,
        IIsbnPageScanner scanner,
        IBookRepository bookRepository,
        BookSaver bookSaver,
        TimeProvider clock,
        CancellationToken ct)
    {
        var scan = await scanner.ScanArchiveAsync(book.FilePath, ct);
        if (!scan.Completed)
        {
            return IsbnScanOutcome.NotScanned;
        }

        // An assigned ISBN is written into the ComicInfo.xml of the archive too.
        var previousComicInfo = BookComicInfo.From(book);
        var outcome = await ApplyAsync(book, scan.Isbns, bookRepository, clock.GetUtcNow().UtcDateTime, ct);
        var saveResult = await bookSaver.SaveAsync(book, previousComicInfo, ct);
        return saveResult.IsFailure ? saveResult.Error! : outcome;
    }

    private static async Task<IsbnScanOutcome> ApplyAsync(
        DigitalBook book,
        IReadOnlyList<string> isbns,
        IBookRepository bookRepository,
        DateTime scannedAtUtc,
        CancellationToken ct)
    {
        if (isbns.Count == 1 && await bookRepository.GetByIsbnAsync(isbns[0], book.LibraryId, ct) is null)
        {
            book.RecordIsbnScan([], scannedAtUtc);
            book.AssignIsbn(isbns[0]);
            return IsbnScanOutcome.Assigned;
        }

        // A single ISBN already used by another book is likely a duplicate: the user decides.
        book.RecordIsbnScan(isbns, scannedAtUtc);
        return isbns.Count > 0 ? IsbnScanOutcome.WithCandidates : IsbnScanOutcome.WithoutIsbn;
    }
}

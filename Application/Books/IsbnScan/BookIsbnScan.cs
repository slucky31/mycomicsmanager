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
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        CancellationToken ct)
    {
        var scan = await scanner.ScanArchiveAsync(book.FilePath, ct);
        if (!scan.Completed)
        {
            return IsbnScanOutcome.NotScanned;
        }

        var outcome = await ApplyAsync(book, scan.Isbns, bookRepository, clock.GetUtcNow().UtcDateTime, ct);
        bookRepository.Update(book);
        var saveResult = await unitOfWork.SaveChangesAsync(ct);
        return saveResult.IsFailure ? saveResult.Error! : outcome;
    }

    private static async Task<IsbnScanOutcome> ApplyAsync(
        DigitalBook book,
        IReadOnlyList<string> isbns,
        IBookRepository bookRepository,
        DateTime scannedAtUtc,
        CancellationToken ct)
    {
        if (isbns.Count == 1 && await bookRepository.GetByIsbnAsync(isbns[0], ct) is null)
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

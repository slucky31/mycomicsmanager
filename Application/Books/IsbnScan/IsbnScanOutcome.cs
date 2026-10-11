namespace Application.Books.IsbnScan;

public enum IsbnScanOutcome
{
    // A single ISBN was read and given to the book.
    Assigned,
    // Several ISBNs (or one already used by another book) are left to the user to pick.
    WithCandidates,
    WithoutIsbn,
    // The pages could not be read (OCR disabled or unavailable, archive unreadable).
    NotScanned,
}

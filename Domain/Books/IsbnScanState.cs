namespace Domain.Books;

/// <summary>Where a book stands in the search of the ISBN printed on its pages.</summary>
public enum IsbnScanState
{
    // The book has an ISBN: there is nothing to look for.
    HasIsbn,
    NotScanned,
    // Several ISBNs were read on the pages: the user picks the book's own.
    Candidates,
    // The pages were read but show no ISBN.
    NotFound,
}

public static class IsbnScanStates
{
    public static IsbnScanState Of(string? isbn, int candidateCount, DateTime? scannedAt)
    {
        if (!string.IsNullOrWhiteSpace(isbn))
        {
            return IsbnScanState.HasIsbn;
        }

        if (candidateCount > 0)
        {
            return IsbnScanState.Candidates;
        }

        return scannedAt is null ? IsbnScanState.NotScanned : IsbnScanState.NotFound;
    }
}

namespace Application.Helpers;

/// <summary>
/// The pages of a book where an ISBN is printed: the copyright notice (usually the back of
/// the title page, sometimes the last page) and the barcode on the back cover.
/// </summary>
public static class IsbnScanPages
{
    public const int DefaultPagesFromEachEnd = 5;

    /// <summary>
    /// Returns the zero-based indexes of the first and last <paramref name="pagesFromEachEnd"/> pages,
    /// the most likely ones first, so a scan can stop as soon as an ISBN is found.
    /// </summary>
    public static IReadOnlyList<int> GetScanOrder(int pageCount, int pagesFromEachEnd = DefaultPagesFromEachEnd)
    {
        if (pageCount <= 0 || pagesFromEachEnd <= 0)
        {
            return [];
        }

        var last = pageCount - 1;
        // Back of the title page, then the back cover and the colophon at the end.
        int[] likely = [1, 2, last, last - 1];
        var first = Enumerable.Range(0, pagesFromEachEnd);
        var end = Enumerable.Range(0, pagesFromEachEnd).Select(offset => last - offset);

        return [.. likely
            .Where(index => index < pagesFromEachEnd || index > last - pagesFromEachEnd)
            .Concat(first)
            .Concat(end)
            .Where(index => index >= 0 && index <= last)
            .Distinct()];
    }

    /// <summary>Returns the same pages in reading order, for quick navigation.</summary>
    public static IReadOnlyList<int> GetPagesInReadingOrder(int pageCount, int pagesFromEachEnd = DefaultPagesFromEachEnd) =>
        [.. GetScanOrder(pageCount, pagesFromEachEnd).Order()];
}

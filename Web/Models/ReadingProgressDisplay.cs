namespace Web.Models;

// Where the reader stopped in a book, as the book page and the Reading page show it.
public static class ReadingProgressDisplay
{
    // The reader records the last page shown (zero-based): "Page 68 of 188" and 36 (%), "Page 68" when the number
    // of pages is unknown.
    public static (string Label, int? Percent) Of(int lastReadPage, int? numberOfPages)
    {
        var page = lastReadPage + 1;
        if (numberOfPages is not > 0)
        {
            return ($"Page {page}", null);
        }

        var pageCount = numberOfPages.Value;
        page = Math.Min(page, pageCount);
        return ($"Page {page} of {pageCount}", page * 100 / pageCount);
    }
}

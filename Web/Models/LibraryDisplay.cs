using Domain.Libraries;
using Web.Enums;

namespace Web.Models;

// Texts of the library page bar and of its search.
public static class LibraryDisplay
{
    public static string BookCount(int count) => count == 1 ? "1 book" : $"{count} books";

    public static string SortLabel(BookSortOrder sortOrder) => sortOrder switch
    {
        BookSortOrder.IdAsc => "Oldest first",
        BookSortOrder.SerieAndVolumeAsc => "Series & volume",
        _ => "Newest first",
    };

    public static string ViewLabel(ViewMode viewMode) => viewMode == ViewMode.List ? "List" : "Covers";

    // "24 books · Newest first"; the sort alone while the number of books is unknown (page opened on a search).
    public static string Subtitle(int? bookCount, BookSortOrder sortOrder) =>
        bookCount is { } count ? $"{BookCount(count)} · {SortLabel(sortOrder)}" : SortLabel(sortOrder);

    public static string SearchResults(int count, string searchTerm) =>
        count == 1 ? $"1 book matches “{searchTerm}”" : $"{count} books match “{searchTerm}”";
}

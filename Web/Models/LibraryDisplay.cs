using Domain.Libraries;
using Domain.Primitives;

namespace Web.Models;

// Texts of the library pages: the bar, the search and the form.
public static class LibraryDisplay
{
    public static string BookCount(int count) => count == 1 ? "1 book" : $"{count} books";

    public static string SortLabel(BookSortOrder sortOrder) => sortOrder switch
    {
        BookSortOrder.IdAsc => "Oldest first",
        BookSortOrder.SerieAndVolumeAsc => "Series & volume",
        _ => "Newest first",
    };

    public static string ViewLabel(LibraryView view) => view switch
    {
        LibraryView.List => "List",
        LibraryView.Series => "Series",
        _ => "Covers",
    };

    // The books of a library, even when it opens on its series (#1210).
    public static string BooksHref(Guid libraryId) => $"/libraries/{libraryId}?books=true";

    // "24 books · Newest first"; the sort alone while the number of books is unknown (page opened on a search).
    public static string Subtitle(int? bookCount, BookSortOrder sortOrder) =>
        bookCount is { } count ? $"{BookCount(count)} · {SortLabel(sortOrder)}" : SortLabel(sortOrder);

    public static string SearchResults(int count, string searchTerm) =>
        count == 1 ? $"1 book matches “{searchTerm}”" : $"{count} books match “{searchTerm}”";

    // Why a library could not be created or saved: its name is taken, or what the form was doing.
    public static string SaveFailed(TError? error, string fallback) =>
        error == LibrariesError.Duplicate ? "Another library already has this name" : fallback;
}

using Microsoft.AspNetCore.WebUtilities;

namespace Web.Models;

// The screen a book was opened from (#1261), carried in the "from" query parameter so that leaving the
// reader or the book page goes back there, even after a reload.
public static class ReturnLinks
{
    public const string QueryName = "from";

    public const string ReadingHref = "/reading";

    // The link is rebuilt from the screens a book can be opened from, never followed as given:
    // anything else ("https://…", "//host", another page) is dropped, so it cannot leave the site (open redirect).
    public static string? Local(string? from)
    {
        if (string.IsNullOrWhiteSpace(from) || from[0] != '/')
        {
            return null;
        }

        var queryStart = from.IndexOf('?', StringComparison.Ordinal);
        var path = queryStart < 0 ? from : from[..queryStart];
        var query = queryStart < 0 ? [] : QueryHelpers.ParseQuery(from[queryStart..]);
        string? Query(string name) => query.TryGetValue(name, out var value) ? value.ToString() : null;

        return path.Split('/', StringSplitOptions.RemoveEmptyEntries) switch
        {
            ["reading"] when path == ReadingHref => ReadingHref,
            ["books", var id] when Guid.TryParse(id, out var bookId) => Book(bookId, Query(QueryName)),
            ["libraries", var id] when Guid.TryParse(id, out var libraryId) => $"/libraries/{libraryId}",
            ["libraries", var id, "series"] when Guid.TryParse(id, out var libraryId) => SerieDisplay.SeriesHref(libraryId),
            ["libraries", var id, "serie"] when Guid.TryParse(id, out var libraryId) && !string.IsNullOrWhiteSpace(Query("name")) =>
                SerieDisplay.SerieHref(libraryId, Query("name")!),
            _ => null,
        };
    }

    // The path with the screen to go back to, left out when it is not one a book can be opened from.
    public static string With(string path, string? from)
    {
        if (Local(from) is not { } local)
        {
            return path;
        }

        var separator = path.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{path}{separator}{QueryName}={Uri.EscapeDataString(local)}";
    }

    public static string Book(Guid bookId, string? from) => With($"/books/{bookId}", from);

    public static string Read(Guid bookId, string? from) => With($"/books/{bookId}/read", from);
}

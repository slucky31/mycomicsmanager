namespace Web.Models;

// The screen a book was opened from (#1261), carried in the "from" query parameter so that leaving the
// reader or the book page goes back there, even after a reload.
public static class ReturnLinks
{
    public const string QueryName = "from";

    public const string ReadingHref = "/reading";

    // Only a path of this site is kept: "https://…", "//host" or "/\host" would leave it (open redirect).
    public static string? Local(string? from)
    {
        if (string.IsNullOrWhiteSpace(from) || from[0] != '/')
        {
            return null;
        }

        if (from.Length > 1 && from[1] is '/' or '\\')
        {
            return null;
        }

        return from.Any(char.IsControl) ? null : from;
    }

    // The path with the screen to go back to, left out when it is not a path of this site.
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

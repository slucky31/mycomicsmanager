namespace Web.Models;

// Which navigation entry is active and when the bottom bar of a phone is hidden, from the base-relative URI.
public static class NavigationRules
{
    private const string LibrariesPrefix = "libraries/";

    // Adding a book is a focused task: on a phone the bottom bar is hidden so it cannot be left by mistake.
    // The rail and the side bar of a tablet or a desktop stay, they do not take the room of the page.
    private static readonly string[] s_pathsWithoutBottomBar = ["books/add"];

    // So is importing a book's info from the web (books/{id}/find-details).
    private const string FindDetailsSuffix = "/find-details";

    public static string StripQueryAndFragment(string relativePath)
    {
        ArgumentNullException.ThrowIfNull(relativePath);

        var queryOrFragmentIndex = relativePath.IndexOfAny(['?', '#']);
        return queryOrFragmentIndex >= 0 ? relativePath[..queryOrFragmentIndex] : relativePath;
    }

    public static bool IsActive(string relativePath, string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var current = StripQueryAndFragment(relativePath);
        var trimmed = path.TrimStart('/');
        if (string.IsNullOrEmpty(trimmed))
        {
            return string.IsNullOrEmpty(current);
        }

        return current.Equals(trimmed, StringComparison.OrdinalIgnoreCase)
            || current.StartsWith(trimmed + "/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool HidesBottomBar(string relativePath)
    {
        var current = StripQueryAndFragment(relativePath);
        return s_pathsWithoutBottomBar.Any(path => IsActive(current, path))
            || current.TrimEnd('/').EndsWith(FindDetailsSuffix, StringComparison.OrdinalIgnoreCase);
    }

    // The library shown by the current page (its books, its settings), if any.
    public static Guid? CurrentLibraryId(string relativePath)
    {
        var current = StripQueryAndFragment(relativePath);
        if (!current.StartsWith(LibrariesPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var segment = current[LibrariesPrefix.Length..].Split('/')[0];
        return Guid.TryParse(segment, out var libraryId) ? libraryId : null;
    }
}

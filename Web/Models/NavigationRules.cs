namespace Web.Models;

// Which navigation entry is active and when the bottom bar of a phone is hidden, from the base-relative URI.
public static class NavigationRules
{
    private const string LibrariesPrefix = "libraries/";

    // A form is a focused task (#1286): on a phone the bottom bar is hidden so it cannot be left by mistake,
    // and so it does not cover the Save and Cancel actions kept at the bottom of the screen (FormFooter).
    // The rail and the side bar of a tablet or a desktop stay, they do not take the room of the page.
    private static readonly string[] s_pathsWithoutBottomBar = ["books/add", "libraries/add"];

    // Importing a book's info from the web (books/{id}/find-details) and editing a book or a library ({id}/edit).
    private static readonly string[] s_suffixesWithoutBottomBar = ["/find-details", "/edit"];

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
            || s_suffixesWithoutBottomBar.Any(suffix => current.TrimEnd('/').EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
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

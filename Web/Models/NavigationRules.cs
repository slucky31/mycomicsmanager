namespace Web.Models;

// Which navigation entry is active and when the navigation is hidden, from the base-relative URI.
public static class NavigationRules
{
    private const string LibrariesPrefix = "libraries/";

    // Adding a book is a focused task: the navigation is hidden so it cannot be left by mistake.
    private static readonly string[] s_pathsWithoutNavigation = ["books/add"];

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

    public static bool HidesNavigation(string relativePath)
    {
        var current = StripQueryAndFragment(relativePath);
        return s_pathsWithoutNavigation.Any(path => IsActive(current, path));
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

namespace Persistence.LocalStorage;

/// <summary>
/// Shared path-traversal guard: confirms a resolved path stays strictly inside a root directory.
/// A candidate equal to the root itself is deliberately rejected (not "within" it), since callers
/// use this to gate destructive operations (delete/move) where the root itself must never qualify.
/// </summary>
internal static class PathContainment
{
    public static string NormalizeRoot(string rootDirectory) =>
        Path.GetFullPath(rootDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

    public static bool IsWithin(string rootDirectory, string candidatePath, StringComparison comparison = StringComparison.OrdinalIgnoreCase) =>
        IsWithinNormalizedRoot(NormalizeRoot(rootDirectory), candidatePath, comparison);

    /// <summary>
    /// Same check as <see cref="IsWithin"/>, but takes a root already normalized via
    /// <see cref="NormalizeRoot"/> — use this in a loop to avoid re-normalizing the root every call.
    /// </summary>
    public static bool IsWithinNormalizedRoot(string normalizedRootWithSeparator, string candidatePath, StringComparison comparison = StringComparison.OrdinalIgnoreCase) =>
        Path.GetFullPath(candidatePath).StartsWith(normalizedRootWithSeparator, comparison);
}

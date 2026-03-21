namespace Persistence.Queries.Helpers;

internal static class LikePatternHelper
{
    /// <summary>
    /// Escapes backslash, percent, and underscore so they are treated as literals
    /// in a PostgreSQL LIKE / ILike pattern that uses '\' as the escape character.
    /// </summary>
    internal static string EscapeLikeSpecialChars(string value)
        => value
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
}

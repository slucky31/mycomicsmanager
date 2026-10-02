using System.Text;
using Domain.Extensions;

namespace Domain.FeedImports;

public static class SerieMatcher
{
    // Below this similarity two series are considered different; at or above it (but not equal) the match is "probable".
    public const double ProbableMatchThreshold = 0.85;

    private static readonly HashSet<string> s_leadingArticles = new(["LE", "LA", "LES", "L", "UN", "UNE", "DES", "THE", "A"], StringComparer.Ordinal);

    // Upper-case, without diacritics, punctuation or leading article: "L'Arabe du futur" -> "ARABE DU FUTUR".
    public static string Normalize(string? serie)
    {
        if (string.IsNullOrWhiteSpace(serie))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(serie.Length);
        foreach (var c in serie.RemoveDiacritics().ToUpperInvariant())
        {
            builder.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }

        var words = builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (words.Count > 1 && s_leadingArticles.Contains(words[0]))
        {
            words.RemoveAt(0);
        }

        return string.Join(' ', words);
    }

    // 1 for identical normalized series, 0 when nothing in common (normalized Levenshtein ratio).
    public static double Similarity(string? left, string? right)
    {
        var a = Normalize(left);
        var b = Normalize(right);
        if (a.Length == 0 || b.Length == 0)
        {
            return 0;
        }

        if (a == b)
        {
            return 1;
        }

        return 1 - ((double)LevenshteinDistance(a, b) / Math.Max(a.Length, b.Length));
    }

    private static int LevenshteinDistance(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }
            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}

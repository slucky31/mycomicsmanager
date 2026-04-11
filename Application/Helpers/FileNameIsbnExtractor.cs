using System.Text.RegularExpressions;

namespace Application.Helpers;

public static partial class FileNameIsbnExtractor
{
    // Matches: ISBN-9781234567890  ISBN_9781234567890  ISBN 9781234567890
    [GeneratedRegex(@"ISBN[-_ ](\d[\d\-]{8,17}\d)", RegexOptions.IgnoreCase)]
    private static partial Regex IsbnPrefixPattern();

    // Matches: (978-1-23-456789-0) or (9781234567890) — parentheses with optional dashes
    [GeneratedRegex(@"\((\d[\d\-]{8,17}\d)\)")]
    private static partial Regex IsbnInParenthesesPattern();

    // Matches: [9781234567890] — brackets
    [GeneratedRegex(@"\[(\d[\d\-]{8,17}\d)\]")]
    private static partial Regex IsbnInBracketsPattern();

    // Matches a raw 13-digit sequence starting with 978 or 979
    [GeneratedRegex(@"\b(97[89]\d{10})\b")]
    private static partial Regex Isbn13SequencePattern();

    // Matches a raw 10-digit sequence
    [GeneratedRegex(@"\b(\d{9}[\dXx])\b")]
    private static partial Regex Isbn10SequencePattern();

    public static string? ExtractIsbn(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var name = Path.GetFileNameWithoutExtension(fileName);

        // 1. Explicit ISBN prefix: ISBN-xxx, ISBN_xxx, ISBN xxx
        var candidate = TryMatch(IsbnPrefixPattern(), name);
        if (candidate is not null) { return candidate; }

        // 2. ISBN between parentheses: (xxx)
        candidate = TryMatch(IsbnInParenthesesPattern(), name);
        if (candidate is not null) { return candidate; }

        // 3. ISBN between brackets: [xxx]
        candidate = TryMatch(IsbnInBracketsPattern(), name);
        if (candidate is not null) { return candidate; }

        // 4. Raw 13-digit sequence starting with 978/979
        candidate = TryMatch(Isbn13SequencePattern(), name);
        if (candidate is not null) { return candidate; }

        // 5. Raw 10-digit sequence
        candidate = TryMatch(Isbn10SequencePattern(), name);
        if (candidate is not null) { return candidate; }

        return null;
    }

    private static string? TryMatch(Regex pattern, string input)
    {
        var match = pattern.Match(input);
        if (!match.Success) { return null; }

        // Normalize: remove dashes and spaces, uppercase
        var raw = match.Groups[1].Value;
        var normalized = IsbnHelper.NormalizeIsbn(raw);

        return IsbnHelper.IsValidISBN(normalized) ? normalized : null;
    }
}

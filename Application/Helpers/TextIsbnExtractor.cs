using System.Text;
using System.Text.RegularExpressions;

namespace Application.Helpers;

/// <summary>
/// Finds the ISBNs printed in a text read by OCR on a book page (usually the copyright notice).
/// Only numbers with a valid checksum are returned, so OCR noise never yields a wrong ISBN.
/// </summary>
public static partial class TextIsbnExtractor
{
    // Characters an OCR engine commonly reads in place of a digit, only trusted right after the "ISBN" label.
    private static readonly Dictionary<char, char> s_digitLookalikes = new()
    {
        ['O'] = '0',
        ['o'] = '0',
        ['D'] = '0',
        ['I'] = '1',
        ['l'] = '1',
        ['|'] = '1',
        ['Z'] = '2',
        ['S'] = '5',
        ['B'] = '8',
    };

    // "ISBN", "ISBN-13 :", "ISBN 10", "I S B N" followed by up to 24 digit-like characters and separators.
    // The "10"/"13" of the label is only skipped when a separator follows, so it is never read as the ISBN's first digits.
    [GeneratedRegex(@"[Ii]\s?[Ss]\s?[Bb]\s?[Nn](?:[\s\-]?1[03](?=[\s:]))?[\s\-:.]*(?<number>[\dOoDIl|ZSBXx][\dOoDIl|ZSBXx\s\-\u2010-\u2015.]{8,24})")]
    private static partial Regex LabelledIsbnPattern();

    // A bare ISBN-13, with at most one separator between digits (e.g. "9 782800 112343" under the barcode).
    [GeneratedRegex(@"(?<!\d)9[\s\-\u2010-\u2015.]?7[\s\-\u2010-\u2015.]?[89](?:[\s\-\u2010-\u2015.]?\d){10}(?!\d)")]
    private static partial Regex BareIsbn13Pattern();

    // A bare ISBN-10 is only trusted when hyphenated, otherwise any 10-digit number would match.
    [GeneratedRegex(@"(?<![\d\-])\d{1,5}-\d{1,7}-\d{1,7}-[\dXx](?![\d\-])")]
    private static partial Regex BareHyphenatedIsbn10Pattern();

    /// <summary>
    /// Returns the distinct valid ISBNs found in <paramref name="text"/>, normalized (no separators),
    /// the ones introduced by an "ISBN" label first.
    /// </summary>
    public static IReadOnlyList<string> ExtractAll(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var isbns = new List<string>();

        foreach (Match match in LabelledIsbnPattern().Matches(text))
        {
            AddIfNew(isbns, ReadLabelledIsbn(match.Groups["number"].Value));
        }

        foreach (Match match in BareIsbn13Pattern().Matches(text))
        {
            AddIfNew(isbns, ValidOrNull(match.Value));
        }

        foreach (Match match in BareHyphenatedIsbn10Pattern().Matches(text))
        {
            AddIfNew(isbns, ValidOrNull(match.Value));
        }

        return isbns;
    }

    // The captured text may run past the ISBN ("978-2-8001-1234-3 Imprimé..."):
    // the ISBN is the longest valid prefix of its digits.
    private static string? ReadLabelledIsbn(string captured)
    {
        var digits = ToDigits(captured);
        if (digits.StartsWith("978", StringComparison.Ordinal) || digits.StartsWith("979", StringComparison.Ordinal))
        {
            // A misread ISBN-13 must not be taken for the ISBN-10 made of its first ten digits.
            return digits.Length >= 13 && IsbnHelper.IsValidISBN13(digits[..13]) ? digits[..13] : null;
        }

        if (digits.Length >= 10 && IsbnHelper.IsValidISBN10(digits[..10]))
        {
            return digits[..10];
        }

        return null;
    }

    private static string ToDigits(string captured)
    {
        var digits = new StringBuilder(captured.Length);
        foreach (var c in captured)
        {
            if (char.IsAsciiDigit(c))
            {
                digits.Append(c);
            }
            else if (c is 'X' or 'x')
            {
                // Only an ISBN-10 check digit: whatever follows is not part of the number.
                digits.Append('X');
                break;
            }
            else if (s_digitLookalikes.TryGetValue(c, out var digit))
            {
                digits.Append(digit);
            }
        }

        return digits.ToString();
    }

    private static string? ValidOrNull(string candidate)
    {
        var normalized = NormalizeSeparators(candidate);
        return IsbnHelper.IsValidISBN(normalized) ? normalized : null;
    }

    private static string NormalizeSeparators(string candidate)
    {
        var normalized = new StringBuilder(candidate.Length);
        foreach (var c in candidate)
        {
            if (char.IsAsciiDigit(c))
            {
                normalized.Append(c);
            }
            else if (c is 'X' or 'x')
            {
                normalized.Append('X');
            }
        }

        return normalized.ToString();
    }

    private static void AddIfNew(List<string> isbns, string? isbn)
    {
        if (isbn is not null && !isbns.Contains(isbn))
        {
            isbns.Add(isbn);
        }
    }
}

using System.Globalization;
using System.Text.RegularExpressions;

namespace Domain.FeedImports;

// Best-effort parsing of an article title or a file name into serie / volume / title,
// e.g. "Blacksad - Tome 3 - Âme rouge", "Blacksad_T03_Ame_rouge.cbz", "Astérix #40".
public static partial class ComicTitleParser
{
    private static readonly string[] s_fileExtensions = [".cbz", ".cbr", ".zip", ".rar", ".pdf", ".epub", ".cb7", ".7z"];

    // "Tomes 1 à 5", "T01-T05", "Tome 1 à 3": a range, not a single volume.
    [GeneratedRegex(@"\b(?:tomes?|t|vol(?:ume)?s?\.?)\s*\d{1,4}\s*(?:à|a|-|–|au)\s*(?:t\s*)?\d{1,4}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VolumeRangePattern();

    // "Tome 3", "T03", "T.3", "Vol. 105", "Volume 2", "#40", "n°12"
    [GeneratedRegex(@"(?:\b(?:tome|vol(?:ume)?\.?|t\.?)\s*|#\s*|\bn°\s*)0*(\d{1,4})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VolumePattern();

    // "Largo Winch - 20 - Le prix de l'argent", "Largo Winch – 20"
    [GeneratedRegex(@"\s[-–—:]\s*0*(\d{1,4})(?=\s*(?:[-–—:]|$))", RegexOptions.CultureInvariant)]
    private static partial Regex DashedNumberPattern();

    [GeneratedRegex(@"[\[(]([^\])]*)[\])]", RegexOptions.CultureInvariant)]
    private static partial Regex BracketGroupPattern();

    [GeneratedRegex(@"\s[-–—:]\s", RegexOptions.CultureInvariant)]
    private static partial Regex SeparatorPattern();

    [GeneratedRegex(@"\s{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex MultiSpacePattern();

    private static readonly char[] s_trimChars = [' ', '-', '–', '—', ':', ',', '.', '_', '|'];

    public static ParsedComicTitle Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return ParsedComicTitle.Empty;
        }

        var text = Clean(input);
        if (text.Length == 0)
        {
            return ParsedComicTitle.Empty;
        }

        if (VolumeRangePattern().IsMatch(text))
        {
            var rangeMatch = VolumeRangePattern().Match(text);
            return new ParsedComicTitle(NullIfEmpty(text[..rangeMatch.Index]), null, null);
        }

        var match = VolumePattern().Match(text);
        if (!match.Success)
        {
            match = DashedNumberPattern().Match(text);
        }

        if (match.Success)
        {
            var serie = NullIfEmpty(text[..match.Index]);
            var title = NullIfEmpty(text[(match.Index + match.Length)..]);
            var volume = int.Parse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture);
            return new ParsedComicTitle(serie, title, volume);
        }

        var parts = SeparatorPattern().Split(text, 2);
        return parts.Length == 2
            ? new ParsedComicTitle(NullIfEmpty(parts[0]), NullIfEmpty(parts[1]), null)
            : new ParsedComicTitle(NullIfEmpty(text), null, null);
    }

    private static string Clean(string input)
    {
        var text = input.Trim();
        var extension = Path.GetExtension(text);
        if (s_fileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            text = text[..^extension.Length];
        }

        // File names often use '_' or '.' instead of spaces.
        text = text.Replace('_', ' ');
        if (!text.Contains(' ', StringComparison.Ordinal))
        {
            text = text.Replace('.', ' ');
        }

        // Keep bracket groups that carry the volume ("(Tome 12)"), drop the others ("[FR]", "(2024)").
        text = BracketGroupPattern().Replace(text, m =>
            VolumePattern().IsMatch(m.Groups[1].Value) || VolumeRangePattern().IsMatch(m.Groups[1].Value)
                ? " - " + m.Groups[1].Value + " "
                : " ");

        return MultiSpacePattern().Replace(text, " ").Trim(s_trimChars);
    }

    private static string? NullIfEmpty(string value)
    {
        var trimmed = MultiSpacePattern().Replace(value, " ").Trim(s_trimChars);
        return trimmed.Length == 0 ? null : trimmed;
    }
}

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

    // "Tome 3", "T03", "T.3", "Vol. 105", "Volume 2", "#40", "n°12", "n56"
    [GeneratedRegex(@"(?:\b(?:tome|vol(?:ume)?\.?|t\.?)\s*|#\s*|\bn°\s*|\bn(?=\d))0*(\d{1,4})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VolumePattern();

    // "Largo Winch - 20 - Le prix de l'argent", "Largo Winch – 20"
    [GeneratedRegex(@"\s[-–—:]\s*0*(\d{1,4})(?=\s*(?:[-–—:]|$))", RegexOptions.CultureInvariant)]
    private static partial Regex DashedNumberPattern();

    // "XIII 01 - Le jour du soleil noir", "Les Vieux Fourneaux 08": at most 3 digits, so a year is not a volume.
    [GeneratedRegex(@"(?<=\s)0*(\d{1,3})(?=\s+[-–—:]|$)", RegexOptions.CultureInvariant)]
    private static partial Regex TrailingNumberPattern();

    // "Ashita_no_Joe_T08@BD_fr": what follows '@' is the tag of whoever shared the file.
    [GeneratedRegex(@"@.*$", RegexOptions.CultureInvariant)]
    private static partial Regex ReleaseTagPattern();

    // "Blacksad T03 zz", "zzmurenaT03": "zz" is a release marker, as a word of its own or glued to the start.
    [GeneratedRegex(@"^zz(?!z)|\bzz\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReleaseMarkerPattern();

    // "murenaT03LaMeilleure", "ALatelier": a space before each word of a camelCase name.
    [GeneratedRegex(@"(?<=\p{Ll})(?=\p{Lu})|(?<=\d)(?=\p{L})|(?<=\p{Lu})(?=\p{Lu}\p{Ll})", RegexOptions.CultureInvariant)]
    private static partial Regex CamelCaseBoundaryPattern();

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

        if (!match.Success)
        {
            match = TrailingNumberPattern().Match(text);
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

        // Uploads sometimes carry a doubled extension ("Londres.cbz.cbz").
        var extension = Path.GetExtension(text);
        while (s_fileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            text = text[..^extension.Length];
            extension = Path.GetExtension(text);
        }

        text = ReleaseTagPattern().Replace(text, string.Empty);

        // File names often use '_' or '.' instead of spaces.
        text = text.Replace('_', ' ');
        if (!text.Contains(' ', StringComparison.Ordinal))
        {
            text = text.Replace('.', ' ');
        }

        text = ReleaseMarkerPattern().Replace(text, " ").Trim();
        if (!text.Contains(' ', StringComparison.Ordinal))
        {
            text = SplitCamelCase(text);
        }

        // Keep bracket groups that carry the volume ("(Tome 12)"), drop the others ("[FR]", "(2024)").
        text = BracketGroupPattern().Replace(text, m =>
            VolumePattern().IsMatch(m.Groups[1].Value) || VolumeRangePattern().IsMatch(m.Groups[1].Value)
                ? " - " + m.Groups[1].Value + " "
                : " ");

        return MultiSpacePattern().Replace(text, " ").Trim(s_trimChars);
    }

    private static string SplitCamelCase(string text)
    {
        var split = CamelCaseBoundaryPattern().Replace(text, " ");
        return split.Length == text.Length || split.Length == 0
            ? text
            : char.ToUpperInvariant(split[0]) + split[1..];
    }

    private static string? NullIfEmpty(string value)
    {
        var trimmed = MultiSpacePattern().Replace(value, " ").Trim(s_trimChars);
        return trimmed.Length == 0 ? null : trimmed;
    }
}

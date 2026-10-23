using System.Globalization;
using System.Text.RegularExpressions;

namespace Application.Helpers;

// Reads the series a catalogue gives apart from the title, with or without its volume.
public static partial class SeriesHelper
{
    // "Blacksad ; 2", "Blacksad, tome 2", "Blacksad -- vol. 2", "Les Aventures d'Adèle Blanc-Sec ; [6]".
    [GeneratedRegex(@"^(?<serie>.+?)\s*(?:;|,|--)\s*(?:(?:tome|t\.|vol\.?|volume|n°|no\.?)\s*)?\[?(?<volume>\d+)\]?$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SerieWithVolumePattern();

    [GeneratedRegex(@"\d+", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex NumberPattern();

    public static (string Serie, int? VolumeNumber) ParseSerie(string value)
    {
        var trimmed = value.Trim();
        var match = SerieWithVolumePattern().Match(trimmed);
        return match.Success
            ? (match.Groups["serie"].Value.Trim(), ParseVolumeNumber(match.Groups["volume"].Value))
            : (trimmed, null);
    }

    // "[6]", "T. 6", "6 bis" → 6; a volume without a number is unknown.
    public static int? ParseVolumeNumber(string? value)
    {
        var match = value is null ? Match.Empty : NumberPattern().Match(value);
        return match.Success && int.TryParse(match.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var volume) ? volume : null;
    }
}

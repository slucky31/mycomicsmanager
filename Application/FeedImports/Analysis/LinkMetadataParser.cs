using System.Globalization;
using System.Text.RegularExpressions;

namespace Application.FeedImports.Analysis;

public static partial class LinkMetadataParser
{
    private static readonly string[] s_fileExtensions = [".cbz", ".cbr", ".zip", ".rar", ".pdf", ".epub", ".cb7", ".7z"];

    // "125 Mo", "1,2 Go", "300MB", "1.5 GB"
    [GeneratedRegex(@"(\d+(?:[.,]\d+)?)\s*(Go|Mo|Ko|GB|MB|KB|GiB|MiB|KiB)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SizePattern();

    // "Blacksad T03.cbz" inside a longer text
    [GeneratedRegex(@"[^\s/\\:*?""<>|][^/\\:*?""<>|]*?\.(?:cbz|cbr|zip|rar|pdf|epub|cb7|7z)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FileNameInTextPattern();

    // Returns a file name (with extension) found in the link text or in the URL path, never a path.
    public static string? FindFileName(string? text, Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);

        if (!string.IsNullOrWhiteSpace(text))
        {
            var match = FileNameInTextPattern().Match(text);
            if (match.Success)
            {
                return Path.GetFileName(match.Value.Trim());
            }
        }

        // e.g. https://rapidgator.net/file/abc123/Blacksad_T03.cbz.html
        foreach (var segment in url.Segments.Reverse())
        {
            var decoded = Uri.UnescapeDataString(segment.Trim('/'));
            if (decoded.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                decoded = decoded[..^".html".Length];
            }

            if (s_fileExtensions.Contains(Path.GetExtension(decoded), StringComparer.OrdinalIgnoreCase))
            {
                return Path.GetFileName(decoded);
            }
        }

        return null;
    }

    public static long? FindSizeBytes(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = SizePattern().Match(text);
        if (!match.Success ||
            !double.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        var multiplier = char.ToUpperInvariant(match.Groups[2].Value[0]) switch
        {
            'G' => 1024d * 1024 * 1024,
            'M' => 1024d * 1024,
            _ => 1024d
        };
        return (long)(value * multiplier);
    }
}

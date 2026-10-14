namespace Web.Models;

// Short texts shared by the book screens (ADR-0021).
public static class BookText
{
    // "Delcourt · 2019 · 271 pages": the parts that are known, separated by a dot.
    public static string? Join(params string?[] parts)
    {
        var shown = parts.Where(part => !string.IsNullOrWhiteSpace(part)).ToArray();
        return shown.Length == 0 ? null : string.Join(" · ", shown);
    }

    // "Blacksad · Volume 4"; the series is left out when it is the title (a one-shot).
    public static string? SerieAndVolume(string? serie, string title, int volumeNumber)
    {
        var serieShown = !string.IsNullOrWhiteSpace(serie)
            && !string.Equals(serie.Trim(), title.Trim(), StringComparison.OrdinalIgnoreCase);
        return Join(serieShown ? serie : null, VolumeLabel(volumeNumber));
    }

    public static string? VolumeLabel(int volumeNumber) => volumeNumber > 0 ? $"Volume {volumeNumber}" : null;

    public static string? PagesLabel(int? numberOfPages) => numberOfPages switch
    {
        1 => "1 page",
        > 1 => $"{numberOfPages} pages",
        _ => null,
    };
}

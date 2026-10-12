using System.Globalization;

namespace Web.Models;

public static class ReaderDisplay
{
    // Many titles already contain the series name ("Blacksad - Arctic Nation"): it is not repeated.
    public static string Title(string? serie, string? title)
    {
        var trimmedSerie = serie?.Trim() ?? string.Empty;
        var trimmedTitle = title?.Trim() ?? string.Empty;

        if (trimmedTitle.Length == 0)
        {
            return trimmedSerie;
        }

        if (trimmedSerie.Length == 0 || trimmedTitle.Contains(trimmedSerie, StringComparison.OrdinalIgnoreCase))
        {
            return trimmedTitle;
        }

        return $"{trimmedSerie} · {trimmedTitle}";
    }

    // Width of the thin progress line shown while the controls are hidden.
    public static string ProgressWidth(int pageIndex, int pageCount)
    {
        if (pageCount <= 0)
        {
            return "0%";
        }

        var percent = Math.Clamp((pageIndex + 1) * 100d / pageCount, 0d, 100d);
        return string.Create(CultureInfo.InvariantCulture, $"{percent:0.##}%");
    }
}

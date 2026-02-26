using System.Globalization;

namespace Application.Helpers;

public static class PublishDateHelper
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext(typeof(PublishDateHelper));

    public static DateOnly? ParsePublishDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return null;
        }

        // OpenLibrary returns dates in various formats:
        // "September 16, 1987", "1987", "Sep 1987", "1987-09-16", etc.
        var formats = new[]
        {
            "MMMM d, yyyy",      // "September 16, 1987"
            "MMMM dd, yyyy",     // "September 16, 1987"
            "MMM d, yyyy",       // "Sep 16, 1987"
            "MMM dd, yyyy",      // "Sep 16, 1987"
            "yyyy-MM-dd",        // "1987-09-16"
            "yyyy/MM/dd",        // "1987/09/16"
            "dd/MM/yyyy",        // "16/09/1987"
            "MM/dd/yyyy",        // "09/16/1987"
            "MMMM yyyy",         // "September 1987"
            "MMM yyyy",          // "Sep 1987"
            "yyyy",              // "1987"
        };

        foreach (var format in formats)
        {
            if (DateOnly.TryParseExact(dateString.Trim(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        // Try generic parsing as fallback
        if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        Log.Warning("Unable to parse publish date: {DateString}", dateString);
        return null;
    }
}

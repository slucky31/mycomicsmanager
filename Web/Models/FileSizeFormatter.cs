using System.Globalization;

namespace Web.Models;

public static class FileSizeFormatter
{
    private static readonly string[] s_sizeUnits = ["B", "KB", "MB", "GB", "TB"];

    public static string Format(long bytes)
    {
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < s_sizeUnits.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        var format = unit == 0 ? "0" : "0.#";
        return $"{size.ToString(format, CultureInfo.InvariantCulture)} {s_sizeUnits[unit]}";
    }
}

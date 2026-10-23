using Domain.Series;

namespace Web.Models;

// Texts and links of the series pages (#1205).
public static class SerieDisplay
{
    // "#4", "#16–19": the volumes are numbered as on the covers.
    public static string RangeLabel(VolumeRange range)
    {
        ArgumentNullException.ThrowIfNull(range);
        return range.First == range.Last ? $"#{range.First}" : $"#{range.First}–{range.Last}";
    }

    public static string VolumesLabel(int first, int last) => first == last ? $"Vol. {first}" : $"Vol. {first}–{last}";

    public static string MissingLabel(int count) => $"{count} missing";

    public static string SeriesCount(int count) => count == 1 ? "1 series" : $"{count} series";

    public static string VolumeCount(int count) => count == 1 ? "1 volume" : $"{count} volumes";

    public static string SerieHref(Guid libraryId, string name) =>
        $"/libraries/{libraryId}/serie?name={Uri.EscapeDataString(name)}";

    public static string SeriesHref(Guid libraryId) => $"/libraries/{libraryId}/series";

    public const string MissingVolumesHref = "/missing-volumes";
}

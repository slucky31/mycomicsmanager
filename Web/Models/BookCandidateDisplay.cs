using System.Globalization;
using Application.Interfaces;

namespace Web.Models;

// How an edition found by a text search (#1237) is shown on the "Import info from the web" page.
public static class BookCandidateDisplay
{
    // "Astérix · #3", the series alone, or null when the source does not give it.
    public static string? SerieAndVolume(BookCandidate candidate)
    {
        var serie = string.IsNullOrWhiteSpace(candidate.Serie) ? null : candidate.Serie.Trim();
        var volume = candidate.VolumeNumber is > 0 ? $"#{candidate.VolumeNumber.Value.ToString(CultureInfo.InvariantCulture)}" : null;
        return Join(serie, volume);
    }

    // "Hachette · 2004": what tells the editions of a book apart.
    public static string? Edition(BookCandidate candidate) =>
        Join(candidate.Publishers.FirstOrDefault(), candidate.PublishDate?.Year.ToString(CultureInfo.InvariantCulture));

    public static string Isbn(BookCandidate candidate) =>
        candidate.Isbn is null ? "No ISBN: this edition cannot be picked" : $"ISBN {candidate.Isbn}";

    // What a source found, next to its name.
    public static string Summary(ProviderSearchState state, int count) => state switch
    {
        ProviderSearchState.Searching => "searching…",
        ProviderSearchState.Found => count == 1 ? "1 edition" : $"{count} editions",
        ProviderSearchState.NotFound => "nothing found",
        _ => "could not be searched",
    };

    private static string? Join(string? first, string? second) =>
        (first, second) switch
        {
            (null, null) => null,
            (null, _) => second,
            (_, null) => first,
            _ => $"{first} · {second}",
        };
}

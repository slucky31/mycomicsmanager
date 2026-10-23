using Domain.Books;

namespace Domain.Series;

// The books of a series are grouped by its name, whatever its case and the spaces around it (#1205).
public static class SerieNames
{
    public static string Normalize(string? serie) => (serie ?? string.Empty).Trim().ToUpperInvariant();

    // The books that are not part of a series (#1171): never complete nor missing anything.
    public static bool IsOneShot(string? serie) => Normalize(serie) == Normalize(BookConstants.OneShotSerie);
}

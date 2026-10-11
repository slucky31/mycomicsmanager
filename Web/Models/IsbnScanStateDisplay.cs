using Domain.Books;
using MudBlazor;

namespace Web.Models;

// How a book shows its ISBN: a QR code, full when the ISBN is known, faded with a badge
// telling why when it is missing.
public static class IsbnScanStateDisplay
{
    public static string Label(IsbnScanState state, int candidateCount = 0) => state switch
    {
        IsbnScanState.NotScanned => "No ISBN: the pages were not scanned yet",
        IsbnScanState.Candidates => $"{candidateCount} ISBNs found in the pages: pick the book's own",
        IsbnScanState.NotFound => "No ISBN found in the pages",
        _ => string.Empty,
    };

    // Shown on the book page next to the scan actions.
    public static string Icon(IsbnScanState state) => state switch
    {
        IsbnScanState.NotScanned => Icons.Material.Filled.DocumentScanner,
        IsbnScanState.Candidates => Icons.Material.Filled.Rule,
        IsbnScanState.NotFound => Icons.Material.Filled.SearchOff,
        _ => string.Empty,
    };

    // The badge content: "?" before any scan, the number of ISBNs to pick from; none otherwise.
    public static string? BadgeContent(IsbnScanState state, int candidateCount) => state switch
    {
        IsbnScanState.NotScanned => "?",
        IsbnScanState.Candidates => candidateCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => null,
    };

    // Nothing was found: the badge is a cross instead of a text.
    public static string? BadgeIcon(IsbnScanState state) =>
        state == IsbnScanState.NotFound ? Icons.Material.Filled.Close : null;

    // Only the candidates wait for the user.
    public static Color BadgeColor(IsbnScanState state) => state switch
    {
        IsbnScanState.Candidates => MudBlazor.Color.Warning,
        IsbnScanState.NotFound => MudBlazor.Color.Error,
        _ => MudBlazor.Color.Dark,
    };
}

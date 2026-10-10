using Domain.Books;
using MudBlazor;

namespace Web.Models;

// How the library views show a book whose ISBN is missing.
public static class IsbnScanStateDisplay
{
    public static string Label(IsbnScanState state) => state switch
    {
        IsbnScanState.NotScanned => "No ISBN: the pages were not scanned yet",
        IsbnScanState.Candidates => "Several ISBNs found in the pages: pick the book's own",
        IsbnScanState.NotFound => "No ISBN found in the pages",
        _ => string.Empty,
    };

    public static string Icon(IsbnScanState state) => state switch
    {
        IsbnScanState.NotScanned => Icons.Material.Filled.QrCodeScanner,
        IsbnScanState.Candidates => Icons.Material.Filled.Rule,
        IsbnScanState.NotFound => Icons.Material.Filled.SearchOff,
        _ => string.Empty,
    };

    // Only the candidates wait for the user.
    public static Color Color(IsbnScanState state) =>
        state == IsbnScanState.Candidates ? MudBlazor.Color.Warning : MudBlazor.Color.Default;
}

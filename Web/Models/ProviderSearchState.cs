using Application.Interfaces;
using MudBlazor;

namespace Web.Models;

// Where the search of one source (BnF, Google Books...) stands on the import page.
public enum ProviderSearchState
{
    Searching,
    Found,
    NotFound,
    Failed,
}

public static class ProviderSearchStates
{
    // A null result: the search threw before the source could answer.
    public static ProviderSearchState Of(IBookSearchResult? result) =>
        result is null ? ProviderSearchState.Failed : Of(result.Found, result.Failed);

    public static ProviderSearchState Of(BedethequeBookResult? result) =>
        result is null ? ProviderSearchState.Failed : Of(result.Found, result.Failed);

    private static ProviderSearchState Of(bool found, bool failed) => (found, failed) switch
    {
        (_, true) => ProviderSearchState.Failed,
        (true, _) => ProviderSearchState.Found,
        _ => ProviderSearchState.NotFound,
    };
}

// Each state has its own icon, so a missing value ("—") tells whether the source is still
// searching, does not know the book or could not be searched.
public static class ProviderSearchStateDisplay
{
    public static string Icon(ProviderSearchState state) => state switch
    {
        ProviderSearchState.Searching => Icons.Material.Filled.HourglassTop,
        ProviderSearchState.Found => Icons.Material.Filled.CheckCircle,
        ProviderSearchState.NotFound => Icons.Material.Filled.SearchOff,
        _ => Icons.Material.Filled.ErrorOutline,
    };

    public static Color Color(ProviderSearchState state) => state switch
    {
        ProviderSearchState.Searching => MudBlazor.Color.Info,
        ProviderSearchState.Found => MudBlazor.Color.Success,
        ProviderSearchState.NotFound => MudBlazor.Color.Default,
        _ => MudBlazor.Color.Warning,
    };

    public static string Label(ProviderSearchState state, string source) => state switch
    {
        ProviderSearchState.Searching => $"{source}: searching…",
        ProviderSearchState.Found => $"{source}: book found",
        ProviderSearchState.NotFound => $"{source}: book not found",
        _ => $"{source}: could not be searched (error, timeout or blocked)",
    };
}

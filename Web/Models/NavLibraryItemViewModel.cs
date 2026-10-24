using Application.Libraries.Summaries;

namespace Web.Models;

// A library entry of the side navigation, with its link and labels computed once.
public sealed record NavLibraryItemViewModel(
    Guid Id,
    string Name,
    string Color,
    string Icon,
    string Href,
    string CountLabel,
    string AriaLabel,
    bool IsHidden = false)
{
    public string CssClass => IsHidden ? "side-nav-library side-nav-library-hidden" : "side-nav-library";

    public static NavLibraryItemViewModel From(LibrarySummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var books = summary.BookCount == 1 ? "1 book" : $"{summary.BookCount} books";
        var hidden = summary.IsHidden ? ", hidden" : string.Empty;
        return new NavLibraryItemViewModel(
            summary.Id,
            summary.Name,
            summary.Color,
            summary.Icon,
            $"/libraries/{summary.Id}",
            summary.BookCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            $"{summary.Name}{hidden}, {books}",
            summary.IsHidden);
    }
}

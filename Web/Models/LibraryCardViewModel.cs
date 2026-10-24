using Application.Libraries.Summaries;
using Domain.Libraries;

namespace Web.Models;

// A library card of the libraries page, with its link and labels computed once.
public sealed record LibraryCardViewModel(
    Guid Id,
    string Name,
    string Color,
    string Icon,
    bool IsDigital,
    int BookCount,
    IReadOnlyList<string> Covers)
{
    public string Href => $"/libraries/{Id}";

    public string EditHref => $"/libraries/{Id}/edit";

    public string ImportHref => $"/import?libraryId={Id}";

    public string TypeLabel => IsDigital ? "Digital" : "Physical";

    public string CountLabel => BookCount == 0 ? "No books yet" : LibraryDisplay.BookCount(BookCount);

    // "Physical · 24 books", under the name in the actions.
    public string Details => $"{TypeLabel} · {CountLabel}";

    public string ActionsLabel => $"More actions for {Name}";

    public string ActionsButtonId => $"library-actions-{Id}";

    // Listed only when the user shows the hidden libraries (#1215).
    public bool IsHidden { get; init; }

    public string CssClass => IsHidden ? "library-card library-card-hidden" : "library-card";

    // The feed imports deposit their files in the Imported library.
    public bool CanDelete => !Library.IsImportedName(Name);

    public static LibraryCardViewModel From(LibrarySummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return new LibraryCardViewModel(
            summary.Id,
            summary.Name,
            summary.Color,
            summary.Icon,
            summary.BookType == LibraryBookType.Digital,
            summary.BookCount,
            summary.Covers)
        {
            IsHidden = summary.IsHidden
        };
    }

    // The card of the library being created or edited (E3, T12): "New library" until it has a name.
    public static LibraryCardViewModel Preview(Guid id, string? name, string color, string icon, LibraryBookType bookType, int bookCount, IReadOnlyList<string> covers) =>
        new(id, string.IsNullOrWhiteSpace(name) ? "New library" : name.Trim(), color, icon, bookType == LibraryBookType.Digital, bookCount, covers);

    // "3 libraries · 48 books", under the page title on a large screen.
    public static string Subtitle(IReadOnlyCollection<LibraryCardViewModel> libraries)
    {
        ArgumentNullException.ThrowIfNull(libraries);

        var count = libraries.Count == 1 ? "1 library" : $"{libraries.Count} libraries";
        return $"{count} · {LibraryDisplay.BookCount(libraries.Sum(l => l.BookCount))}";
    }
}

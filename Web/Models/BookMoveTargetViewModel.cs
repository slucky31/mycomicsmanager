using System.Globalization;
using Application.Books.MoveTargets;
using Domain.Libraries;

namespace Web.Models;

// A library of the dialog to move a book (E6, T14): "Digital · 112 books", the current one shown but not chosen.
public sealed record BookMoveTargetViewModel(Guid Id, string Name, string Color, string Icon, string Details, bool IsSuggested, bool IsCurrent = false)
{
    public string MoveLabel => $"Move to {Name}";
}

// The libraries of the dialog, the current one first, and the one chosen when the dialog opens.
public sealed record BookMoveChoices(IReadOnlyList<BookMoveTargetViewModel> Libraries, string? CurrentName)
{
    public bool HasTargets => Libraries.Any(l => !l.IsCurrent);

    public Guid? Preselected => Libraries.FirstOrDefault(l => l.IsSuggested)?.Id;

    public static BookMoveChoices From(BookMoveTargets targets)
    {
        ArgumentNullException.ThrowIfNull(targets);

        var type = targets.BookType == LibraryBookType.Digital ? "Digital" : "Physical";
        List<BookMoveTargetViewModel> libraries = [];
        if (targets.Current is { } current)
        {
            libraries.Add(new BookMoveTargetViewModel(current.Id, current.Name, current.Color, current.Icon, $"{type} · current library", false, true));
        }

        libraries.AddRange(targets.Libraries.Select(l => new BookMoveTargetViewModel(
            l.Id,
            l.Name,
            l.Color,
            l.Icon,
            BookText.Join(type, l.BookCount == 0 ? "No books yet" : LibraryDisplay.BookCount(l.BookCount), SameSerieLabel(l.SameSerieCount))!,
            l.Id == targets.SuggestedLibraryId)));

        return new BookMoveChoices(libraries, targets.Current?.Name);
    }

    private static string? SameSerieLabel(int count) => count switch
    {
        0 => null,
        1 => "1 of this series",
        _ => string.Create(CultureInfo.InvariantCulture, $"{count} of this series"),
    };
}

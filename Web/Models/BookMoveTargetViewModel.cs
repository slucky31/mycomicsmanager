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
public sealed record BookMoveChoices(IReadOnlyList<BookMoveTargetViewModel> Libraries, string? CurrentName, int BookCount = 1)
{
    public bool HasTargets => Libraries.Any(l => !l.IsCurrent);

    // "This book is in" Read Books, "These 3 books are in" Read Books.
    public string Subject => BookCount == 1 ? "This book is in" : $"These {BookCount} books are in";

    public string FileNote => BookCount == 1 ? "Its file is moved too." : "Their files are moved too.";

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
            BookText.Join(type, l.BookCount == 0 ? "No books yet" : LibraryDisplay.BookCount(l.BookCount), SameSerieLabel(l.SameSerieCount, targets.BookCount))!,
            l.Id == targets.SuggestedLibraryId)));

        return new BookMoveChoices(libraries, targets.Current?.Name, targets.BookCount);
    }

    private static string? SameSerieLabel(int count, int bookCount)
    {
        if (count == 0)
        {
            return null;
        }

        var series = bookCount == 1 ? "this series" : "these series";
        return string.Create(CultureInfo.InvariantCulture, $"{count} of {series}");
    }
}

using System.Globalization;
using Domain.Books;

namespace Web.Models;

// One reading of the book being edited: "12 March 2026" and its stars.
public sealed record EditBookReading(Guid Id, string Date, int Rating)
{
    public string DeleteLabel => $"Delete the reading of {Date}";

    // The question before the reading is deleted (E5).
    public string DeleteMessage => $"The reading of {Date}, rated {Rating} out of 5, will be removed. This cannot be undone.";
}

// What the form to edit a book shows besides its fields, computed once from the book (E1, E2, T11).
public sealed record EditBookViewModel
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string ImageLink { get; init; }
    public required bool IsDigital { get; init; }

    // "Extremity · Volume 1" under "Edit book".
    public required string Subtitle { get; init; }

    public IReadOnlyList<EditBookReading> Readings { get; init; } = [];

    public string BookHref => $"/books/{Id}";
    public string CoverAlt => $"Cover of {Title}";

    public string ReadingsCountLabel => Readings.Count == 1 ? "1 reading" : $"{Readings.Count} readings";

    // "Read Books · Physical"; the library may not be known when it could not be loaded.
    public string LibraryLabel(string? libraryName) => BookText.Join(libraryName ?? "Library", IsDigital ? "Digital" : "Physical")!;

    public static EditBookViewModel From(Book book)
    {
        ArgumentNullException.ThrowIfNull(book);

        return new EditBookViewModel
        {
            Id = book.Id,
            Title = book.Title,
            ImageLink = book.ImageLink,
            IsDigital = book is DigitalBook,
            Subtitle = BookText.Join(book.Title, BookText.VolumeLabel(book.VolumeNumber))!,
            Readings = [.. book.ReadingDates
                .OrderByDescending(rd => rd.Date)
                .ThenBy(rd => rd.Id)
                .Select(rd => new EditBookReading(rd.Id, rd.Date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture), rd.Rating))],
        };
    }
}

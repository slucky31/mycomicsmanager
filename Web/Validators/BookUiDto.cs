using Domain.Books;
using Domain.Primitives;
using MudBlazor;

namespace Web.Validators;

public class BookUiDto : Entity<Guid>
{
    [Label("Serie")]
    public string Serie { get; set; } = string.Empty;

    [Label("Title")]
    public string Title { get; set; } = string.Empty;

    [Label("ISBN")]
    public string? ISBN { get; set; }

    [Label("Volume Number")]
    public int VolumeNumber { get; set; } = 1;

    [Label("Image Link")]
    public string ImageLink { get; set; } = string.Empty;

    [Label("Rating")]
    public int Rating { get; set; } = 0;

    [Label("Authors")]
    public string Authors { get; set; } = string.Empty;

    [Label("Publishers")]
    public string Publishers { get; set; } = string.Empty;

    [Label("Publish Date")]
    public DateOnly? PublishDate { get; set; }

    [Label("Number of Pages")]
    public int? NumberOfPages { get; set; }

    [Label("Library")]
    public Guid LibraryId { get; set; } = Guid.Empty;

    // Physical books require an ISBN (Domain/Books/PhysicalBook.cs); digital books imported
    // without a metadata match may not have one. Defaults to true for the manual "add book" flow.
    public bool IsPhysical { get; set; } = true;

    // The cover of a digital book is the first page of its archive (#1193): it is only typed when the book has none.
    public bool HasArchiveCover { get; private set; }

    // A one-shot is not part of a series: its series is "One Shot" and its volume 1, neither can be changed (#1171).
    public bool IsOneShot { get; private set; }

    // What was typed before the book was made a one-shot, given back when it is no longer one.
    private (string Serie, int VolumeNumber)? _beforeOneShot;

    public void SetOneShot(bool isOneShot)
    {
        if (isOneShot == IsOneShot)
        {
            return;
        }

        IsOneShot = isOneShot;
        if (isOneShot)
        {
            _beforeOneShot = (Serie, VolumeNumber);
            Serie = BookConstants.OneShotSerie;
            VolumeNumber = 1;
        }
        else if (_beforeOneShot is { } before)
        {
            (Serie, VolumeNumber) = before;
            _beforeOneShot = null;
        }
    }

    public static BookUiDto Convert(Book book)
    {
        return new BookUiDto
        {
            Id = book.Id,
            Serie = book.Serie,
            Title = book.Title,
            ISBN = book.ISBN,
            IsPhysical = book is PhysicalBook,
            HasArchiveCover = book is DigitalBook && !string.IsNullOrWhiteSpace(book.ImageLink),
            IsOneShot = IsOneShotBook(book),
            VolumeNumber = book.VolumeNumber,
            ImageLink = book.ImageLink,
            Rating = 0,
            Authors = book.Authors,
            Publishers = book.Publishers,
            PublishDate = book.PublishDate,
            NumberOfPages = book.NumberOfPages,
            LibraryId = book.LibraryId,
            CreatedOnUtc = book.CreatedOnUtc,
            ModifiedOnUtc = book.ModifiedOnUtc
        };
    }

    private static bool IsOneShotBook(Book book) =>
        book.VolumeNumber == 1 && string.Equals(book.Serie, BookConstants.OneShotSerie, StringComparison.OrdinalIgnoreCase);
}

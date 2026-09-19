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

    public static BookUiDto Convert(Book book)
    {
        return new BookUiDto
        {
            Id = book.Id,
            Serie = book.Serie,
            Title = book.Title,
            ISBN = book.ISBN,
            IsPhysical = book is PhysicalBook,
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
}

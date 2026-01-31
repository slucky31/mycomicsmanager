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
    public string ISBN { get; set; } = string.Empty;

    [Label("Volume Number")]
    public int VolumeNumber { get; set; } = 1;

    [Label("Image Link")]
    public string ImageLink { get; set; } = string.Empty;

    [Label("Rating")]
    public int Rating { get; set; } = 0;

    public static BookUiDto Convert(Book book)
    {
        return new BookUiDto
        {
            Id = book.Id,
            Serie = book.Serie,
            Title = book.Title,
            ISBN = book.ISBN,
            VolumeNumber = book.VolumeNumber,
            ImageLink = book.ImageLink,
            Rating = book.Rating,
            CreatedOnUtc = book.CreatedOnUtc,
            ModifiedOnUtc = book.ModifiedOnUtc
        };
    }
}

using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;
using MudBlazor;

namespace Web.Validators;

public class BookUiDto : Entity<Guid>
{
    [Label("Series")]
    public string Series { get; set; } = string.Empty;

    [Label("Title")]
    public string Title { get; set; } = string.Empty;

    [Label("ISBN")]
    public string ISBN { get; set; } = string.Empty;

    [Label("Volume Number")]
    public int VolumeNumber { get; set; } = 1;

    [Label("Image Link")]
    public string ImageLink { get; set; } = string.Empty;

    public static BookUiDto Convert(Book book)
    {
        Guard.Against.Null(book);

        return new BookUiDto
        {
            Id = book.Id,
            Series = book.Series,
            Title = book.Title,
            ISBN = book.ISBN,
            VolumeNumber = book.VolumeNumber,
            ImageLink = book.ImageLink,
            CreatedOnUtc = book.CreatedOnUtc,
            ModifiedOnUtc = book.ModifiedOnUtc
        };
    }
}
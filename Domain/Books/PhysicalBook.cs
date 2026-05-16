using Domain.Primitives;

namespace Domain.Books;

public sealed class PhysicalBook : Book
{
    private PhysicalBook() { }

    public static Result<PhysicalBook> Create(BookMetadata metadata, Guid libraryId)
    {
        if (string.IsNullOrWhiteSpace(metadata.Serie) ||
            string.IsNullOrWhiteSpace(metadata.Title) ||
            string.IsNullOrWhiteSpace(metadata.ISBN) ||
            libraryId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        var book = new PhysicalBook
        {
            Id = Guid.CreateVersion7(),
            LibraryId = libraryId,
            Serie = metadata.Serie,
            Title = metadata.Title,
            ISBN = metadata.ISBN,
            VolumeNumber = metadata.VolumeNumber,
            ImageLink = metadata.ImageLink,
            Authors = metadata.Authors,
            Publishers = metadata.Publishers,
            PublishDate = metadata.PublishDate,
            NumberOfPages = metadata.NumberOfPages
        };

        return book;
    }
}

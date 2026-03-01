using Domain.Primitives;

namespace Domain.Books;

public sealed class PhysicalBook : Book
{
    private PhysicalBook() { }

    public static Result<PhysicalBook> Create(
        string serie,
        string title,
        string isbn,
        int volumeNumber = 1,
        string imageLink = "",
        string authors = "",
        string publishers = "",
        DateOnly? publishDate = null,
        int? numberOfPages = null,
        Guid libraryId = default)
    {
        if (string.IsNullOrWhiteSpace(serie) ||
            string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(isbn) ||
            libraryId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        var book = new PhysicalBook
        {
            Id = Guid.CreateVersion7(),
            LibraryId = libraryId,
            Serie = serie,
            Title = title,
            ISBN = isbn,
            VolumeNumber = volumeNumber,
            ImageLink = imageLink,
            Authors = authors,
            Publishers = publishers,
            PublishDate = publishDate,
            NumberOfPages = numberOfPages
        };

        return book;
    }
}

using Domain.Primitives;

namespace Domain.Books;

public sealed class DigitalBook : Book
{
    public string FilePath { get; private set; } = string.Empty;

    public long FileSize { get; private set; }

    public DigitalBook() { }

    public static Result<DigitalBook> Create(
        string serie,
        string title,
        string? isbn,
        Guid libraryId,
        string filePath,
        long fileSize,
        int volumeNumber = 1,
        string imageLink = "",
        string authors = "",
        string publishers = "",
        DateOnly? publishDate = null,
        int? numberOfPages = null)
    {
        if (string.IsNullOrWhiteSpace(serie) ||
            string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(filePath) ||
            libraryId == Guid.Empty ||
            fileSize <= 0)
        {
            return BooksError.BadRequest;
        }

        var book = new DigitalBook
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
            NumberOfPages = numberOfPages,
            FilePath = filePath,
            FileSize = fileSize
        };

        return book;
    }
}

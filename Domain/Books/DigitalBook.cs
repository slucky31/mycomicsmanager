using Domain.Primitives;

namespace Domain.Books;

public sealed class DigitalBook : Book
{
    public string FilePath { get; private set; } = string.Empty;

    public long FileSize { get; private set; }

    private DigitalBook() { }

    public static Result<DigitalBook> Create(
        BookMetadata metadata,
        Guid libraryId,
        string filePath,
        long fileSize)
    {
        if (string.IsNullOrWhiteSpace(metadata.Serie) ||
            string.IsNullOrWhiteSpace(metadata.Title) ||
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
            Serie = metadata.Serie,
            Title = metadata.Title,
            ISBN = metadata.ISBN,
            VolumeNumber = metadata.VolumeNumber,
            ImageLink = metadata.ImageLink,
            Authors = metadata.Authors,
            Publishers = metadata.Publishers,
            PublishDate = metadata.PublishDate,
            NumberOfPages = metadata.NumberOfPages,
            FilePath = filePath,
            FileSize = fileSize
        };

        return book;
    }
}

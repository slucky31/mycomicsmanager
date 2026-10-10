using Domain.Primitives;

namespace Domain.Books;

public sealed class DigitalBook : Book
{
    public string FilePath { get; private set; } = string.Empty;

    public long FileSize { get; private set; }

    // Zero-based index of the last page displayed in the reader.
    public int LastReadPage { get; private set; }

    // ISBNs read on the pages that could not be assigned automatically (several of them): the user picks one.
    private readonly List<string> _isbnCandidates = [];
    public IReadOnlyList<string> IsbnCandidates => _isbnCandidates.AsReadOnly();

    // When the pages were last scanned for an ISBN; null when they never were.
    public DateTime? IsbnScannedAt { get; private set; }

    private DigitalBook() { }

    public Result RecordIsbnScan(IReadOnlyList<string> candidates, DateTime scannedAtUtc)
    {
        if (candidates is null || candidates.Any(string.IsNullOrWhiteSpace))
        {
            return BooksError.InvalidISBN;
        }

        _isbnCandidates.Clear();
        _isbnCandidates.AddRange(candidates.Distinct(StringComparer.Ordinal));
        IsbnScannedAt = scannedAtUtc;
        return Result.Success();
    }

    // Once the book has an ISBN, the candidates are no longer needed.
    protected override void OnIsbnChanged()
    {
        if (!string.IsNullOrWhiteSpace(ISBN))
        {
            _isbnCandidates.Clear();
        }
    }

    public Result UpdateReadingProgress(int pageIndex)
    {
        if (pageIndex < 0)
        {
            return BooksError.BadRequest;
        }

        LastReadPage = pageIndex;
        return Result.Success();
    }

    // The file has already been moved into the folder of the target library.
    public Result MoveToLibrary(Guid libraryId, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return BooksError.BadRequest;
        }

        var changed = ChangeLibrary(libraryId);
        if (changed.IsFailure)
        {
            return changed;
        }

        FilePath = filePath;
        return Result.Success();
    }

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

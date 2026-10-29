using Domain.Primitives;

namespace Domain.Books;

public sealed class DigitalBook : Book
{
    public string FilePath { get; private set; } = string.Empty;

    public long FileSize { get; private set; }

    // Zero-based index of the last page displayed in the reader.
    public int LastReadPage { get; private set; }

    // When the reader last saved the page (#1252): the books in progress are listed from the last one read.
    public DateTime? LastReadAt { get; private set; }

    // How the reader displays this book, chosen by the reader in its "Display" panel.
    public ReaderLayout ReaderLayout { get; private set; }

    public ReaderScrolling ReaderScrolling { get; private set; }

    public ReadingDirection ReadingDirection { get; private set; }

    public ReaderSettings ReaderSettings => new(ReaderLayout, ReaderScrolling, ReadingDirection);

    // ISBNs read on the pages that could not be assigned automatically (several of them): the user picks one.
    private readonly List<string> _isbnCandidates = [];
    public IReadOnlyList<string> IsbnCandidates => _isbnCandidates.AsReadOnly();

    // When the pages were last scanned for an ISBN; null when they never were.
    public DateTime? IsbnScannedAt { get; private set; }

    private DigitalBook() { }

    public IsbnScanState GetIsbnScanState() => IsbnScanStates.Of(ISBN, _isbnCandidates.Count, IsbnScannedAt);

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

    // The cover and the number of pages come from the archive (#1193): a cover found elsewhere is only taken
    // by a book that has none, when its first page could not be uploaded.
    protected override BookMetadata KeepArchiveMetadata(BookMetadata metadata) => metadata with
    {
        ImageLink = string.IsNullOrWhiteSpace(ImageLink) ? metadata.ImageLink : ImageLink,
        NumberOfPages = NumberOfPages
    };

    // Read again from the archive; a null cover keeps the current one.
    public Result RefreshFromArchive(int pageCount, string? coverLink)
    {
        if (pageCount <= 0)
        {
            return BooksError.BadRequest;
        }

        NumberOfPages = pageCount;
        if (!string.IsNullOrWhiteSpace(coverLink))
        {
            ImageLink = coverLink;
        }

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

    public Result UpdateReadingProgress(int pageIndex, DateTime readAtUtc)
    {
        if (pageIndex < 0)
        {
            return BooksError.BadRequest;
        }

        LastReadPage = pageIndex;
        LastReadAt = readAtUtc;
        return Result.Success();
    }

    public Result UpdateReaderSettings(ReaderSettings settings)
    {
        if (settings is null || !settings.IsValid())
        {
            return BooksError.BadRequest;
        }

        ReaderLayout = settings.Layout;
        ReaderScrolling = settings.Scrolling;
        ReadingDirection = settings.Direction;
        return Result.Success();
    }

    // The folder of its library was renamed: the file is still in it, under a new path.
    public Result Relocate(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return BooksError.BadRequest;
        }

        FilePath = filePath;
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

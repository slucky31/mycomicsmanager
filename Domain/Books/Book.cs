using Domain.Libraries;
using Domain.Primitives;

namespace Domain.Books;

public abstract class Book : Entity<Guid>
{
    public Guid LibraryId { get; protected set; }

    public Library? Library { get; protected set; }

    public string Serie { get; protected set; } = string.Empty;

    public string Title { get; protected set; } = string.Empty;

    public string? ISBN { get; protected set; }

    public int VolumeNumber { get; protected set; } = 1;

    public string ImageLink { get; protected set; } = string.Empty;

    public string Authors { get; protected set; } = string.Empty;

    public string Publishers { get; protected set; } = string.Empty;

    public DateOnly? PublishDate { get; protected set; }

    public int? NumberOfPages { get; protected set; }

    private readonly List<ReadingDate> _readingDates = [];
    public IReadOnlyList<ReadingDate> ReadingDates => _readingDates.AsReadOnly();

    protected Book() { }

    // Checked before any file is moved, so a refused move never leaves the file and the database out of sync.
    public Result CanMoveToLibrary(Guid libraryId)
    {
        if (libraryId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        return libraryId == LibraryId ? BooksError.AlreadyInLibrary : Result.Success();
    }

    protected Result ChangeLibrary(Guid libraryId)
    {
        var check = CanMoveToLibrary(libraryId);
        if (check.IsFailure)
        {
            return check;
        }

        LibraryId = libraryId;
        return Result.Success();
    }

    public Result Update(BookMetadata metadata)
    {
        var validationResult = ValidateMetadataForUpdate(metadata);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        Serie = metadata.Serie;
        Title = metadata.Title;
        ISBN = metadata.ISBN;
        VolumeNumber = metadata.VolumeNumber;
        ImageLink = metadata.ImageLink;
        Authors = metadata.Authors;
        Publishers = metadata.Publishers;
        PublishDate = metadata.PublishDate;
        NumberOfPages = metadata.NumberOfPages;
        OnIsbnChanged();
        return Result.Success();
    }

    // Sets the ISBN alone (e.g. read on the pages of the book), leaving the other metadata untouched.
    public Result AssignIsbn(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn) || isbn.Length > BookConstants.MaxIsbnLength)
        {
            return BooksError.InvalidISBN;
        }

        ISBN = isbn;
        OnIsbnChanged();
        return Result.Success();
    }

    protected virtual Result ValidateMetadataForUpdate(BookMetadata metadata) => Result.Success();

    protected virtual void OnIsbnChanged()
    {
    }

    public ReadingDate AddReadingDate(DateTime date, int rating)
    {
        var readingDate = ReadingDate.Create(date, rating, Id);
        _readingDates.Add(readingDate);
        return readingDate;
    }

    public void RemoveReadingDate(Guid readingDateId)
    {
        var readingDate = _readingDates.Find(rd => rd.Id == readingDateId);
        if (readingDate != null)
        {
            _readingDates.Remove(readingDate);
        }
    }
}

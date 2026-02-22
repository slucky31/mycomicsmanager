using Domain.Primitives;

namespace Domain.Books;

public class Book : Entity<Guid>
{
    public string Serie { get; protected set; } = string.Empty;

    public string Title { get; protected set; } = string.Empty;

    public string ISBN { get; protected set; } = string.Empty;

    public int VolumeNumber { get; protected set; } = 1;

    public string ImageLink { get; protected set; } = string.Empty;

    public string Authors { get; protected set; } = string.Empty;

    public string Publishers { get; protected set; } = string.Empty;

    public DateOnly? PublishDate { get; protected set; }

    public int? NumberOfPages { get; protected set; }

    private readonly List<ReadingDate> _readingDates = [];
    public IReadOnlyList<ReadingDate> ReadingDates => _readingDates.AsReadOnly();

    public static Book Create(
        string series,
        string title,
        string isbn,
        int volumeNumber = 1,
        string imageLink = "",
        string authors = "",
        string publishers = "",
        DateOnly? publishDate = null,
        int? numberOfPages = null)
    {
        var book = new Book
        {
            Id = Guid.CreateVersion7(),
            Serie = series,
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

    public void Update(
        string series,
        string title,
        string isbn,
        int volumeNumber,
        string imageLink,
        string authors = "",
        string publishers = "",
        DateOnly? publishDate = null,
        int? numberOfPages = null)
    {
        Serie = series;
        Title = title;
        ISBN = isbn;
        VolumeNumber = volumeNumber;
        ImageLink = imageLink;
        Authors = authors;
        Publishers = publishers;
        PublishDate = publishDate;
        NumberOfPages = numberOfPages;
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

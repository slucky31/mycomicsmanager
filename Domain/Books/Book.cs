using Domain.Primitives;

namespace Domain.Books;

public class Book : Entity<Guid>
{
    public string Serie { get; protected set; } = string.Empty;

    public string Title { get; protected set; } = string.Empty;

    public string ISBN { get; protected set; } = string.Empty;

    public int VolumeNumber { get; protected set; } = 1;

    public string ImageLink { get; protected set; } = string.Empty;

    public int Rating { get; protected set; }

    public string Authors { get; protected set; } = string.Empty;

    public string Publishers { get; protected set; } = string.Empty;

    public DateOnly? PublishDate { get; protected set; }

    public int? NumberOfPages { get; protected set; }

    private readonly List<ReadingDate> _readingDates = [];
    public IReadOnlyList<ReadingDate> ReadingDates => _readingDates.AsReadOnly();

    public static Book Create(string series, string title, string isbn)
    {
        return Create(series, title, isbn, 1, "", 0, "", "", null, null);
    }

    public static Book Create(string series, string title, string isbn, int volumeNumber)
    {
        return Create(series, title, isbn, volumeNumber, "", 0, "", "", null, null);
    }

    public static Book Create(string series, string title, string isbn, int volumeNumber, string imageLink)
    {
        return Create(series, title, isbn, volumeNumber, imageLink, 0, "", "", null, null);
    }

    public static Book Create(string series, string title, string isbn, int volumeNumber, string imageLink, int rating)
    {
        return Create(series, title, isbn, volumeNumber, imageLink, rating, "", "", null, null);
    }

    public static Book Create(string series, string title, string isbn, int volumeNumber, string imageLink, int rating,
        string authors, string publishers, DateOnly? publishDate, int? numberOfPages)
    {
        var book = new Book
        {
            Id = Guid.CreateVersion7(),
            Serie = series,
            Title = title,
            ISBN = isbn,
            VolumeNumber = volumeNumber,
            ImageLink = imageLink,
            Rating = rating,
            Authors = authors,
            Publishers = publishers,
            PublishDate = publishDate,
            NumberOfPages = numberOfPages
        };
        return book;
    }

    public void Update(string series, string title, string isbn, int volumeNumber, string imageLink, int rating)
    {
        Update(series, title, isbn, volumeNumber, imageLink, rating, Authors, Publishers, PublishDate, NumberOfPages);
    }

    public void Update(string series, string title, string isbn, int volumeNumber, string imageLink, int rating,
        string authors, string publishers, DateOnly? publishDate, int? numberOfPages)
    {
        Serie = series;
        Title = title;
        ISBN = isbn;
        VolumeNumber = volumeNumber;
        ImageLink = imageLink;
        Rating = rating;
        Authors = authors;
        Publishers = publishers;
        PublishDate = publishDate;
        NumberOfPages = numberOfPages;
    }

    public void AddReadingDate(DateTime date)
    {
        var readingDate = ReadingDate.Create(date, Id);
        _readingDates.Add(readingDate);
    }

    public void RemoveReadingDate(Guid readingDateId)
    {
        var readingDate = _readingDates.Find(rd => rd.Id == readingDateId);
        if (readingDate != null)
        {
            _readingDates.Remove(readingDate);
        }
    }

    public void UpdateReadingDate(Guid readingDateId, DateTime date)
    {
        var readingDate = _readingDates.Find(rd => rd.Id == readingDateId);
        readingDate?.Update(date);
    }
}

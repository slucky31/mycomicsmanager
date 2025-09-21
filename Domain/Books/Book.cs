using Domain.Primitives;

namespace Domain.Books;

public class Book : Entity<Guid>
{
    public string Series { get; protected set; } = string.Empty;
    
    public string Title { get; protected set; } = string.Empty;
    
    public string ISBN { get; protected set; } = string.Empty;
    
    public int VolumeNumber { get; protected set; } = 1;
    
    public string ImageLink { get; protected set; } = string.Empty;
    
    private readonly List<ReadingDate> _readingDates = [];
    public IReadOnlyList<ReadingDate> ReadingDates => _readingDates.AsReadOnly();

    public static Book Create(string series, string title, string isbn, int volumeNumber = 1, string imageLink = "")
    {
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Series = series,
            Title = title,
            ISBN = isbn,
            VolumeNumber = volumeNumber,
            ImageLink = imageLink
        };
        return book;
    }

    public void Update(string series, string title, string isbn, int volumeNumber, string imageLink)
    {
        Series = series;
        Title = title;
        ISBN = isbn;
        VolumeNumber = volumeNumber;
        ImageLink = imageLink;
    }

    public void AddReadingDate(DateTime date, string note = "")
    {
        var readingDate = ReadingDate.Create(date, note, Id);
        _readingDates.Add(readingDate);
    }

    public void RemoveReadingDate(Guid readingDateId)
    {
        var readingDate = _readingDates.FirstOrDefault(rd => rd.Id == readingDateId);
        if (readingDate != null)
        {
            _readingDates.Remove(readingDate);
        }
    }

    public void UpdateReadingDate(Guid readingDateId, DateTime date, string note)
    {
        var readingDate = _readingDates.FirstOrDefault(rd => rd.Id == readingDateId);
        if (readingDate != null)
        {
            readingDate.Update(date, note);
        }
    }
}
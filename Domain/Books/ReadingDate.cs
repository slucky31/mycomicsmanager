using Domain.Primitives;

namespace Domain.Books;

public class ReadingDate : Entity<Guid>
{
    public DateTime Date { get; protected set; }
    
    public string Note { get; protected set; } = string.Empty;
    
    public Guid BookId { get; protected set; }

    public static ReadingDate Create(DateTime date, string note, Guid bookId)
    {
        var readingDate = new ReadingDate
        {
            Id = Guid.NewGuid(),
            Date = date,
            Note = note,
            BookId = bookId
        };
        return readingDate;
    }

    public void Update(DateTime date, string note)
    {
        Date = date;
        Note = note;
    }
}
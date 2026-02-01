using Domain.Primitives;

namespace Domain.Books;

public class ReadingDate : Entity<Guid>
{
    public DateTime Date { get; protected set; }

    public Guid BookId { get; protected set; }

    public static ReadingDate Create(DateTime date, Guid bookId)
    {
        var readingDate = new ReadingDate
        {
            Id = Guid.CreateVersion7(),
            Date = date,
            BookId = bookId
        };
        return readingDate;
    }

    public void Update(DateTime date)
    {
        Date = date;
    }
}

using Domain.Primitives;

namespace Domain.Books;

#pragma warning disable CA1054, CA1056 // URL stored as string in database

public sealed class IsbnBedethequeUrl : Entity<Guid>
{
    public string ISBN { get; private set; } = string.Empty;

    public string Url { get; private set; } = string.Empty;

    private IsbnBedethequeUrl() { }

    public static IsbnBedethequeUrl Create(string isbn, string url)
    {
        return new IsbnBedethequeUrl
        {
            Id = Guid.CreateVersion7(),
            ISBN = isbn,
            Url = url,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}
#pragma warning restore CA1054, CA1056

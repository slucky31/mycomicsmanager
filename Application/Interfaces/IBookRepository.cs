using Domain.Books;

namespace Application.Interfaces;

public interface IBookRepository : IRepository<Book, Guid>
{
    Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
    void AddReadingDate(ReadingDate readingDate);
    Task<List<Book>> ListByLibraryIdAsync(Guid libraryId, CancellationToken cancellationToken = default);
    Task<List<Book>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

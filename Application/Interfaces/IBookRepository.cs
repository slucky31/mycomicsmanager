using Domain.Books;

namespace Application.Interfaces;

public interface IBookRepository : IRepository<Book, Guid>
{
    Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
    void AddReadingDate(ReadingDate readingDate);
    Task<List<Book>> ListByLibraryIdAsync(Guid libraryId, CancellationToken cancellationToken = default);
    Task<List<Book>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    // Digital books of the library without ISBN whose pages were never scanned for one.
    Task<List<Guid>> ListIdsToScanForIsbnAsync(Guid libraryId, CancellationToken cancellationToken = default);
}

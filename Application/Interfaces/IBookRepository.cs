using Domain.Books;

namespace Application.Interfaces;

public interface IBookRepository : IRepository<Book, Guid>
{
    // A book of the owner of the library, in a library of the same book type, with this ISBN (with its library):
    // an ISBN is owned at most once in Physical and once in Digital (#1217).
    Task<Book?> GetByIsbnAsync(string isbn, Guid libraryId, CancellationToken cancellationToken = default);
    void AddReadingDate(ReadingDate readingDate);
    Task<List<Book>> ListByLibraryIdAsync(Guid libraryId, CancellationToken cancellationToken = default);
    Task<List<Book>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    // Digital books of the library without ISBN whose pages were never scanned for one.
    Task<List<Guid>> ListIdsToScanForIsbnAsync(Guid libraryId, CancellationToken cancellationToken = default);
}

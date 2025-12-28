using Domain.Books;

namespace Application.Interfaces;

public interface IBookRepository : IRepository<Book, Guid>
{
    Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
}

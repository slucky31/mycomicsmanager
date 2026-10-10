using Application.Books.Read;
using Domain.Primitives;

namespace Web.Services;

public interface IBookReaderService
{
    Task<Result<BookReaderInfoDto>> GetReaderInfoAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<Result> SaveProgressAsync(Guid bookId, int pageIndex, CancellationToken cancellationToken = default);
}

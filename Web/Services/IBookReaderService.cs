using Application.Books.Read;
using Domain.Books;
using Domain.Primitives;

namespace Web.Services;

public interface IBookReaderService
{
    Task<Result<BookReaderInfoDto>> GetReaderInfoAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<Result> SaveProgressAsync(Guid bookId, int pageIndex, CancellationToken cancellationToken = default);

    Task<Result> SaveSettingsAsync(Guid bookId, ReaderSettings settings, CancellationToken cancellationToken = default);
}

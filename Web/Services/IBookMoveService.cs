using Application.Books.MoveTargets;
using Domain.Books;
using Domain.Primitives;

namespace Web.Services;

public interface IBookMoveService
{
    Task<Result<BookMoveTargets>> GetTargetsAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<Result<Book>> MoveAsync(Guid bookId, Guid targetLibraryId, CancellationToken cancellationToken = default);
}

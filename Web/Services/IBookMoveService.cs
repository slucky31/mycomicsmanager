using Application.Books.Move;
using Application.Books.MoveTargets;
using Domain.Books;
using Domain.Primitives;

namespace Web.Services;

public interface IBookMoveService
{
    Task<Result<BookMoveTargets>> GetTargetsAsync(Guid bookId, CancellationToken cancellationToken = default);

    // Books of the same library (#1150).
    Task<Result<BookMoveTargets>> GetTargetsAsync(IReadOnlyList<Guid> bookIds, CancellationToken cancellationToken = default);

    Task<Result<Book>> MoveAsync(Guid bookId, Guid targetLibraryId, CancellationToken cancellationToken = default);

    Task<Result<MoveBooksResult>> MoveAsync(IReadOnlyList<Guid> bookIds, Guid targetLibraryId, CancellationToken cancellationToken = default);
}

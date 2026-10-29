using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Users;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;
using Domain.Users;

namespace Application.Books.Reading;

public sealed class GetBooksInProgressQueryHandler(
    IBookReadService bookReadService,
    IRepository<User, Guid> userRepository) : IQueryHandler<GetBooksInProgressQuery, IReadOnlyList<BookInProgressDto>>
{
    public async Task<Result<IReadOnlyList<BookInProgressDto>>> Handle(GetBooksInProgressQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        if (request.UserId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        // The books of the hidden libraries are listed only when the user shows them (#1215).
        var includeHidden = await userRepository.ShowsHiddenLibrariesAsync(request.UserId);
        var books = await bookReadService.ListInProgressAsync(request.UserId, includeHidden, cancellationToken);

        return Result<IReadOnlyList<BookInProgressDto>>.Success(books);
    }
}

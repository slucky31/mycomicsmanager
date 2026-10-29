using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Users;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;
using Domain.Users;

namespace Application.Books.Reading;

public sealed class GetRecentReadingsQueryHandler(
    IBookReadService bookReadService,
    IRepository<User, Guid> userRepository) : IQueryHandler<GetRecentReadingsQuery, IPagedList<BookReadingDto>>
{
    public async Task<Result<IPagedList<BookReadingDto>>> Handle(GetRecentReadingsQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        if (request.UserId == Guid.Empty || request.Page <= 0 || request.PageSize <= 0)
        {
            return BooksError.BadRequest;
        }

        // The books of the hidden libraries are listed only when the user shows them (#1215).
        var includeHidden = await userRepository.ShowsHiddenLibrariesAsync(request.UserId);
        var readings = await bookReadService.GetRecentReadingsAsync(request.UserId, request.Page, request.PageSize, includeHidden, cancellationToken);

        return Result<IPagedList<BookReadingDto>>.Success(readings);
    }
}

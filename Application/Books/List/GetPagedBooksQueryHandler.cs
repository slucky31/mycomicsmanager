using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.List;

public sealed class GetPagedBooksQueryHandler(
    IBookReadService bookReadService,
    IRepository<Library, Guid> libraryRepository) : IQueryHandler<GetPagedBooksQuery, IPagedList<Book>>
{
    public async Task<Result<IPagedList<Book>>> Handle(GetPagedBooksQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        var library = await libraryRepository.GetByIdAsync(request.LibraryId);
        if (library is null || library.UserId != request.UserId)
        {
            return LibrariesError.NotFound;
        }

        var pagedList = await bookReadService.GetPagedByLibraryAsync(
            request.LibraryId,
            request.UserId,
            request.Page,
            request.PageSize,
            request.SortOrder,
            request.SearchTerm,
            cancellationToken);

        return Result<IPagedList<Book>>.Success(pagedList);
    }
}

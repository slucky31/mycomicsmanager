using Application.Abstractions.Messaging;
using Application.Helpers;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.IsbnOwners;

public sealed class GetIsbnOwnerQueryHandler(
    IBookRepository bookRepository,
    IRepository<Library, Guid> libraryRepository) : IQueryHandler<GetIsbnOwnerQuery, IReadOnlyList<IsbnOwnerDto>>
{
    public async Task<Result<IReadOnlyList<IsbnOwnerDto>>> Handle(GetIsbnOwnerQuery query, CancellationToken cancellationToken)
    {
        if (query is null || query.LibraryId == Guid.Empty || query.UserId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        // A malformed ISBN is reported by the form itself.
        if (string.IsNullOrWhiteSpace(query.Isbn) || !IsbnHelper.IsValidISBN(query.Isbn))
        {
            return Result.Success<IReadOnlyList<IsbnOwnerDto>>([]);
        }

        var library = await libraryRepository.GetByIdAsync(query.LibraryId);
        if (library is null || library.UserId != query.UserId)
        {
            return LibrariesError.NotFound;
        }

        var owner = await bookRepository.GetByIsbnAsync(IsbnHelper.NormalizeIsbn(query.Isbn), query.LibraryId, cancellationToken);
        if (owner is null || owner.Id == query.BookId)
        {
            return Result.Success<IReadOnlyList<IsbnOwnerDto>>([]);
        }

        return Result.Success<IReadOnlyList<IsbnOwnerDto>>(
            [new IsbnOwnerDto(owner.ISBN ?? query.Isbn, owner.Id, owner.Serie, owner.Title, owner.VolumeNumber, owner.ImageLink, owner.Library?.Name ?? string.Empty)]);
    }
}

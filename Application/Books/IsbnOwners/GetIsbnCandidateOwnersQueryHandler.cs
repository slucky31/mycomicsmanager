using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.IsbnOwners;

public sealed class GetIsbnCandidateOwnersQueryHandler(
    IBookRepository bookRepository,
    IRepository<Library, Guid> libraryRepository) : IQueryHandler<GetIsbnCandidateOwnersQuery, IReadOnlyList<IsbnOwnerDto>>
{
    public async Task<Result<IReadOnlyList<IsbnOwnerDto>>> Handle(GetIsbnCandidateOwnersQuery query, CancellationToken cancellationToken)
    {
        if (query is null || query.BookId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        var book = await bookRepository.GetByIdAsync(query.BookId);
        if (book is null)
        {
            return BooksError.NotFound;
        }

        var library = await libraryRepository.GetByIdAsync(book.LibraryId);
        if (library is null || library.UserId != query.UserId)
        {
            return BooksError.NotFound;
        }

        if (book is not DigitalBook digitalBook)
        {
            return Result<IReadOnlyList<IsbnOwnerDto>>.Success([]);
        }

        // A handful of ISBNs at most are read on the pages: one lookup each.
        var owners = new List<IsbnOwnerDto>();
        foreach (var isbn in digitalBook.IsbnCandidates)
        {
            var owner = await bookRepository.GetByIsbnAsync(isbn, book.LibraryId, cancellationToken);
            if (owner is not null && owner.Id != book.Id)
            {
                owners.Add(new IsbnOwnerDto(isbn, owner.Id, owner.Serie, owner.Title, owner.VolumeNumber, owner.ImageLink, owner.Library?.Name ?? string.Empty));
            }
        }

        return Result<IReadOnlyList<IsbnOwnerDto>>.Success(owners);
    }
}

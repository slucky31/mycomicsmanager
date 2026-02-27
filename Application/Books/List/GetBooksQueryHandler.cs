using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.List;

public sealed class GetBooksQueryHandler(
    IBookRepository bookRepository,
    IRepository<Library, Guid> libraryRepository) : IQueryHandler<GetBooksQuery, List<Book>>
{
    public async Task<Result<List<Book>>> Handle(GetBooksQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        if (!request.LibraryId.HasValue)
        {
            return await bookRepository.ListAsync();
        }

        var library = await libraryRepository.GetByIdAsync(request.LibraryId.Value);
        if (library is null || (request.UserId.HasValue && library.UserId != request.UserId.Value))
        {
            return LibrariesError.NotFound;
        }

        return await bookRepository.ListByLibraryIdAsync(request.LibraryId.Value, cancellationToken);
    }
}

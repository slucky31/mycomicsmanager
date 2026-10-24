using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Users;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;
using Domain.Users;

namespace Application.Books.Search;

public sealed class SearchBooksQueryHandler(IBookReadService bookReadService, IRepository<User, Guid> userRepository) : IQueryHandler<SearchBooksQuery, BookSearchResult>
{
    public async Task<Result<BookSearchResult>> Handle(SearchBooksQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        if (string.IsNullOrWhiteSpace(request.SearchTerm) || request.BookLimit <= 0)
        {
            return BooksError.BadRequest;
        }

        // The hidden libraries are searched only when the user shows them (#1215).
        var includeHidden = await userRepository.ShowsHiddenLibrariesAsync(request.UserId);
        var result = await bookReadService.SearchAsync(request.UserId, request.SearchTerm.Trim(), request.BookLimit, includeHidden, cancellationToken);

        return Result<BookSearchResult>.Success(result);
    }
}

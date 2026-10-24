using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;

namespace Application.Books.Search;

public sealed class SearchBooksQueryHandler(IBookReadService bookReadService) : IQueryHandler<SearchBooksQuery, BookSearchResult>
{
    public async Task<Result<BookSearchResult>> Handle(SearchBooksQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        if (string.IsNullOrWhiteSpace(request.SearchTerm) || request.BookLimit <= 0)
        {
            return BooksError.BadRequest;
        }

        var result = await bookReadService.SearchAsync(request.UserId, request.SearchTerm.Trim(), request.BookLimit, cancellationToken);

        return Result<BookSearchResult>.Success(result);
    }
}

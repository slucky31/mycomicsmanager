using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Libraries;

namespace Application.Books.List;

public record GetPagedBooksQuery(
    Guid UserId,
    Guid LibraryId,
    int Page,
    int PageSize,
    BookSortOrder SortOrder,
    string? SearchTerm = null) : IQuery<IPagedList<BookSummaryDto>>;

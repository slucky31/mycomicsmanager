using Application.Abstractions.Messaging;
using Application.Interfaces;

namespace Application.Books.Reading;

// The reading dates of the books of the user, the last one first (#1252).
public record GetRecentReadingsQuery(Guid UserId, int Page, int PageSize) : IQuery<IPagedList<BookReadingDto>>;

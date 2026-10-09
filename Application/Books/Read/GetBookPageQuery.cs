using Application.Abstractions.Messaging;
using Application.Interfaces;

namespace Application.Books.Read;

public record GetBookPageQuery(Guid BookId, Guid UserId, int PageIndex) : IQuery<ComicPage>;

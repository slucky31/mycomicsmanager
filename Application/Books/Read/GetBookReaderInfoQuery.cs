using Application.Abstractions.Messaging;

namespace Application.Books.Read;

public record GetBookReaderInfoQuery(Guid BookId, Guid UserId) : IQuery<BookReaderInfoDto>;

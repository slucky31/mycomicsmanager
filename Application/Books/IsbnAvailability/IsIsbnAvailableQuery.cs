using Application.Abstractions.Messaging;

namespace Application.Books.IsbnAvailability;

// Whether the ISBN can be given to the book: another book of its owner, of the same type (physical or
// digital), may hold it already (#1217). True when it can.
public record IsIsbnAvailableQuery(Guid BookId, string Isbn, Guid UserId) : IQuery<bool>;

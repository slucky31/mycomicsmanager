using Application.Abstractions.Messaging;

namespace Application.Books.Reading;

// The digital books of the user opened in the reader and not finished, the last one read first (#1252).
public record GetBooksInProgressQuery(Guid UserId) : IQuery<IReadOnlyList<BookInProgressDto>>;

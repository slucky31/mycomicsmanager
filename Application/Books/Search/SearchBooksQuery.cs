using Application.Abstractions.Messaging;

namespace Application.Books.Search;

// The series and books of all the libraries of the user whose title or series holds the term (#1209).
public record SearchBooksQuery(Guid UserId, string SearchTerm, int BookLimit) : IQuery<BookSearchResult>;

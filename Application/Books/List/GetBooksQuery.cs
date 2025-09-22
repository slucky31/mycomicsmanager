using Application.Abstractions.Messaging;
using Domain.Books;

namespace Application.Books.List;

public record GetBooksQuery() : IQuery<List<Book>>;
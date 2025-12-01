using Application.Abstractions.Messaging;
using Domain.Books;

namespace Application.Books.GetById;

public record GetBookByIsbnQuery(Guid Id) : IQuery<Book>;

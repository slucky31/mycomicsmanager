using Application.Abstractions.Messaging;
using Domain.Books;

namespace Application.Books.GetById;

public record GetBookQuery(Guid Id) : IQuery<Book>;
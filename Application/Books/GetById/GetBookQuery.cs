using Application.Abstractions.Messaging;
using Domain.Books;

namespace Application.Books.GetById;

public record GetBookByIdQuery(Guid Id, Guid UserId) : IQuery<Book>;

using Application.Abstractions.Messaging;
using Domain.Books;

namespace Application.Books.List;

public record GetBooksQuery(Guid UserId, Guid? LibraryId = null) : IQuery<List<Book>>;

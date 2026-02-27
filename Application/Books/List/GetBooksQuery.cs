using Application.Abstractions.Messaging;
using Domain.Books;

namespace Application.Books.List;

public record GetBooksQuery(Guid? LibraryId = null, Guid? UserId = null) : IQuery<List<Book>>;

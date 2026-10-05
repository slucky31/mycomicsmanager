using Application.Abstractions.Messaging;
using Domain.Books;

namespace Application.Books.Move;

public record MoveBookCommand(Guid BookId, Guid TargetLibraryId, Guid UserId) : ICommand<Book>;

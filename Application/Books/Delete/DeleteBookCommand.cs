using Application.Abstractions.Messaging;

namespace Application.Books.Delete;

public record DeleteBookCommand(Guid Id) : ICommand;

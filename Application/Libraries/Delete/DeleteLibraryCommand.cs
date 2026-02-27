using Application.Abstractions.Messaging;

namespace Application.Libraries.Delete;

public record DeleteLibraryCommand(Guid Id, Guid UserId) : ICommand;

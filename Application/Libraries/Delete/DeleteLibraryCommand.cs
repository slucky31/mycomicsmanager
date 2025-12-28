using Application.Abstractions.Messaging;

namespace Application.Libraries.Delete;

public record DeleteLibraryCommand(Guid Id) : ICommand;

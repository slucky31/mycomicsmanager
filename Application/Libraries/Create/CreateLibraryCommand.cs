using Application.Abstractions.Messaging;
using Domain.Libraries;

namespace Application.Libraries.Create;

public record CreateLibraryCommand(string Name) : ICommand<Library>;

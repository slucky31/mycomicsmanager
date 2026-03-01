using Application.Abstractions.Messaging;
using Domain.Libraries;

namespace Application.Libraries.Create;

public record CreateLibraryCommand(
    string Name,
    string Color,
    string Icon,
    LibraryBookType BookType,
    Guid UserId
) : ICommand<Library>;

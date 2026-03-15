using Application.Abstractions.Messaging;
using Domain.Libraries;

namespace Application.Libraries.Update;

public record UpdateLibraryCommand(
    Guid Id,
    string? Name,
    string Color,
    string Icon,
    Guid UserId,
    BookSortOrder? DefaultBookSortOrder = null
) : ICommand<Library>;

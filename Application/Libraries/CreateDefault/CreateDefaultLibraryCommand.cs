using Application.Abstractions.Messaging;
using Domain.Libraries;

namespace Application.Libraries.CreateDefault;

public record CreateDefaultLibraryCommand(Guid UserId) : ICommand<Library>;

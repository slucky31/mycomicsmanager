using Domain.Libraries;
using Domain.Primitives;
using MediatR;

namespace Application.Librairies.Create;

public record CreateLibraryCommand(string Name) : IRequest<Result<Library>>;

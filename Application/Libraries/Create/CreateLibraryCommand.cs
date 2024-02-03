using Domain.Libraries;
using Domain.Primitives;
using MediatR;

namespace Application.Libraries.Create;

public record CreateLibraryCommand(string Name, string RelPath) : IRequest<Result<Library>>;

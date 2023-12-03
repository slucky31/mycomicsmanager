using MediatR;

namespace Application.Librairies.Create;

public record CreateLibraryCommand(string Name) : IRequest;

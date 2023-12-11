using MediatR;

namespace Application.Librairies.Update;

public record UpdateLibraryCommand (string libraryId, string Name) : IRequest;

public record UpdateLibraryRequest(string Name);

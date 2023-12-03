using Domain.Libraries;
using MediatR;

namespace Application.Librairies.Update;

public record UpdateLibraryCommand (LibraryId libraryId, string Name) : IRequest;

public record UpdateLibraryRequest(string Name);

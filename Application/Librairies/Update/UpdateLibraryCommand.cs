using Domain.Libraries;
using Domain.Primitives;
using MediatR;

namespace Application.Librairies.Update;

public record UpdateLibraryCommand (LibraryId Id, string Name) : IRequest<Result>;

public record UpdateLibraryRequest(string Name);

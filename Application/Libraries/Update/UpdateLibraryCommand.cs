using Domain.Libraries;
using Domain.Primitives;
using MediatR;

namespace Application.Libraries.Update;

public record UpdateLibraryCommand (LibraryId Id, string Name) : IRequest<Result>;

// TODO : bizarre ... pkoi on utilise pas la commande dans l'API
public record UpdateLibraryRequest(string Name);

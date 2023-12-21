using Domain.Libraries;
using Domain.Primitives;
using MediatR;

namespace Application.Librairies.Get;
public record GetLibraryQuery(LibraryId Id) : IRequest<Result<Library>>;

public record LibraryResponse(
    LibraryId Id,
    string Name
);

using Domain.Libraries;
using Domain.Primitives;
using MediatR;

namespace Application.Librairies.Get;
public record GetLibraryQuery(string LibraryId) : IRequest<Result<Library>>;

public record LibraryResponse(
    string Id,
    string Name
);

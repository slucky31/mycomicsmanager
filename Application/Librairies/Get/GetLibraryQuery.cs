using Domain.Libraries;
using MediatR;

namespace Application.Librairies.Get;
public record GetLibraryQuery(string LibraryId) : IRequest<Library>;

public record LibraryResponse(
    string Id,
    string Name
);

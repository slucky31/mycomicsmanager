using Domain.Libraries;
using Domain.Primitives;
using MediatR;

namespace Application.Librairies.Delete;
public record DeleteLibraryCommand(string libraryId) : IRequest<Result>;

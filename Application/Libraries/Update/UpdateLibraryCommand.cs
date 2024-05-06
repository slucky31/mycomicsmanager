using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using MongoDB.Bson;

namespace Application.Libraries.Update;

public record UpdateLibraryCommand (ObjectId Id, string Name) : IRequest<Result<Library>>;

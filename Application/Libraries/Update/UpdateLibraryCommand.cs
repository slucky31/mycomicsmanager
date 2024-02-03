using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using MongoDB.Bson;

namespace Application.Libraries.Update;

public record UpdateLibraryCommand (ObjectId Id, string Name, string relPath) : IRequest<Result>;

public record UpdateLibraryRequest(string Name, string relPath);

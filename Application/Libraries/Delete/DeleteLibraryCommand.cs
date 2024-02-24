using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using MongoDB.Bson;

namespace Application.Libraries.Delete;
public record DeleteLibraryCommand(ObjectId Id) : IRequest<Result>;

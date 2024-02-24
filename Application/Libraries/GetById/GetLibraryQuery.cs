using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using MongoDB.Bson;

namespace Application.Libraries.GetById;
public record GetLibraryQuery(ObjectId Id) : IRequest<Result<Library>>;

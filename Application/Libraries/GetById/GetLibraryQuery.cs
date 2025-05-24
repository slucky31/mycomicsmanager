using Application.Abstractions.Messaging;
using Domain.Libraries;
using MongoDB.Bson;

namespace Application.Libraries.GetById;
public record GetLibraryQuery(ObjectId Id) : IQuery<Library>;

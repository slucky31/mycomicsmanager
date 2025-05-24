using Application.Abstractions.Messaging;
using Domain.Libraries;
using MongoDB.Bson;

namespace Application.Libraries.Update;

public record UpdateLibraryCommand(ObjectId Id, string Name) : ICommand<Library>;

using Application.Abstractions.Messaging;
using MongoDB.Bson;

namespace Application.Libraries.Delete;
public record DeleteLibraryCommand(ObjectId Id) : ICommand;

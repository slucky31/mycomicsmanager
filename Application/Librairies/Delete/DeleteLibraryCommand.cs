using Domain.Libraries;
using MediatR;
using MongoDB.Bson;

namespace Application.Librairies.Delete;
public record DeleteLibraryCommand(string libraryId) : IRequest;

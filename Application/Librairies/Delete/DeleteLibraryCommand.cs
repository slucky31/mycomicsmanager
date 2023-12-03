using Domain.Libraries;
using MediatR;

namespace Application.Librairies.Delete;
public record DeleteLibraryCommand(LibraryId libraryId) : IRequest;

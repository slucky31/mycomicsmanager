using Domain.Libraries;
using Domain.Primitives;
using MediatR;

namespace Application.Libraries.Delete;
public record DeleteLibraryCommand(LibraryId Id) : IRequest<Result>;

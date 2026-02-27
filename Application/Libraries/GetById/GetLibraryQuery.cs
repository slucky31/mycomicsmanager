using Application.Abstractions.Messaging;
using Domain.Libraries;

namespace Application.Libraries.GetById;

public record GetLibraryQuery(Guid Id, Guid UserId) : IQuery<Library>;

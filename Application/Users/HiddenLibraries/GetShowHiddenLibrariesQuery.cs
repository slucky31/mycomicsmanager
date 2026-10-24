using Application.Abstractions.Messaging;

namespace Application.Users.HiddenLibraries;

public sealed record GetShowHiddenLibrariesQuery(Guid UserId) : IQuery<bool>;

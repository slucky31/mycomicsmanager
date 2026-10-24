using Application.Abstractions.Messaging;

namespace Application.Users.HiddenLibraries;

// The setting "Show hidden libraries" of the user (#1215).
public sealed record SetShowHiddenLibrariesCommand(Guid UserId, bool Show) : ICommand;

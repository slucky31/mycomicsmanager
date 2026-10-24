using Application.Abstractions.Messaging;
using Application.Users.HiddenLibraries;

namespace Web.Services;

// The handlers of the setting "Show hidden libraries" (#1215), injected together into LibrariesService.
public sealed record ShowHiddenLibrariesHandlers(
    IQueryHandler<GetShowHiddenLibrariesQuery, bool> Get,
    ICommandHandler<SetShowHiddenLibrariesCommand> Set);

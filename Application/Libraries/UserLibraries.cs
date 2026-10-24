namespace Application.Libraries;

// The libraries of a user, with or without the hidden ones (#1215).
public sealed record UserLibraries(Guid UserId, bool IncludeHidden);

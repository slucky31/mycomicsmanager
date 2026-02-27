namespace Web.Services;

public sealed record UpdateLibraryRequest(
    string? Id,
    string? Name,
    string Color,
    string Icon);

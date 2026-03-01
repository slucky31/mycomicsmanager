using Domain.Libraries;

namespace Web.Services;

public sealed record CreateLibraryRequest(
    string Name,
    string Color,
    string Icon,
    LibraryBookType BookType);

using Domain.Primitives;

namespace Domain.Errors;
public static class LibrariesErrors
{
    public static readonly TError NotFound = new("LIB404", "Library not found");
}

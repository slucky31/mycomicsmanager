using Domain.Primitives;

namespace Domain.Libraries;
public static class LibrariesErrors
{
    public static readonly TError NotFound = new("LIB404", "Library not found");
}

using Domain.Primitives;

namespace Domain.Libraries;
public static class LibrariesErrors
{
    public static readonly TError BadRequest = new("LIB400", "Verify the parameter of the request");
    public static readonly TError NotFound = new("LIB404", "Library not found");    
    public static readonly TError Duplicate = new("LIB409", "A librabry is already created with this name");
}

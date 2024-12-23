using Domain.Primitives;

namespace Domain.Libraries;
public static class LibrariesError
{
    public static readonly TError BadRequest = new("LIB400", "Verify the parameter of the request");
    public static readonly TError NotFound = new("LIB404", "Library not found");    
    public static readonly TError Duplicate = new("LIB409", "A librabry is already created with this name");
    public static readonly TError FolderNotCreated = new("LIB501", "The library directory can not be created");
    public static readonly TError FolderNotDeleted = new("LIB502", "The library directory was not deleted");
    public static readonly TError FolderNotMoved = new("LIB503", "The library directory was not moved");
}

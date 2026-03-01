using Domain.Primitives;

namespace Domain.Errors;

public static class LibraryLocalStorageError
{
    public static readonly TError ArgumentNullOrEmpty = new("ArgumentNullOrEmpty", "path is null.");
    public static readonly TError UnknownFolder = new("UnknownFolder", "The folder doesn't exist.");
    public static readonly TError AlreadyExistingFolder = new("AlreadyExistingFolder", "The destination directory already exists.");
    public static readonly TError InvalidPath = new("InvalidPath", "The path is outside the allowed root directory.");
}

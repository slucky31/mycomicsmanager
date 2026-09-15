using Domain.Primitives;

namespace Domain.Errors;

public static class ImportDirectoryStorageError
{
    public static readonly TError ArgumentNullOrEmpty = new("ImportDir.ArgumentNullOrEmpty", "Directory name is null or empty.");
    public static readonly TError InvalidPath = new("ImportDir.InvalidPath", "The path is outside the allowed import root directory.");
}

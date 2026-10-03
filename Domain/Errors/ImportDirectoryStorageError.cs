using Domain.Primitives;

namespace Domain.Errors;

public static class ImportDirectoryStorageError
{
    public static readonly TError ArgumentNullOrEmpty = new("ImportDir.ArgumentNullOrEmpty", "Directory name is null or empty.");
    public static readonly TError InvalidPath = new("ImportDir.InvalidPath", "The path is outside the allowed import root directory.");
    public static readonly TError DestinationAlreadyExists = new("ImportDir.DestinationAlreadyExists", "A directory already exists at the destination path.");
    public static readonly TError FileAlreadyExists = new("ImportDir.FileAlreadyExists", "A file already exists at the destination path.");
    public static readonly TError SourceFileNotFound = new("ImportDir.SourceFileNotFound", "The file to deposit does not exist.");
    public static readonly TError InvalidFileName = new("ImportDir.InvalidFileName", "The file name is empty or invalid.");
    public static readonly TError DepositFailed = new("ImportDir.DepositFailed", "The file could not be moved into the import directory.");
}

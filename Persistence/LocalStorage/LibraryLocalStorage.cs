using System.Text;
using Application.Libraries;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Errors;
using Domain.Extensions;
using Domain.Primitives;

namespace Persistence.LocalStorage;

public class LibraryLocalStorage : ILibraryLocalStorage
{
    private readonly char[] _charsToTrim = ['/', '\\'];

    public string rootPath { get; init; }

    public LibraryLocalStorage(string rootPath)
    {
        this.rootPath = rootPath;
    }

    private Result ValidatePath(string folderName)
    {
        try
        {
            var fullPath = Path.GetFullPath(Path.Combine(rootPath, folderName));
            if (!PathContainment.IsWithin(rootPath, fullPath))
            {
                return LibraryLocalStorageError.InvalidPath;
            }
            return Result.Success();
        }
        catch (Exception ex) when (
            ex is ArgumentException or
            NotSupportedException or
            PathTooLongException)
        {
            return LibraryLocalStorageError.InvalidPath;
        }
    }

    public Result Create(string folderName)
    {
        Guard.Against.Null(rootPath);
        Guard.Against.Null(folderName);

        if (string.IsNullOrEmpty(rootPath) || string.IsNullOrEmpty(folderName))
        {
            return LibraryLocalStorageError.ArgumentNullOrEmpty;
        }

        var pathValidation = ValidatePath(folderName);
        if (pathValidation.IsFailure)
        {
            return pathValidation.Error!;
        }

        var path = new StringBuilder();
        path.Append(rootPath.TrimEnd(_charsToTrim)).Append(Path.DirectorySeparatorChar).Append(folderName);

        Directory.CreateDirectory(path.ToString());
        return Result.Success();
    }

    public Result Move(string originFolderName, string destinationFolderName)
    {
        Guard.Against.Null(rootPath);
        Guard.Against.Null(originFolderName);
        Guard.Against.Null(destinationFolderName);

        if (string.IsNullOrEmpty(rootPath) || string.IsNullOrEmpty(originFolderName) || string.IsNullOrEmpty(destinationFolderName))
        {
            return LibraryLocalStorageError.ArgumentNullOrEmpty;
        }

        var sanitizedOrigin = originFolderName.RemoveDiacritics();
        var originValidation = ValidatePath(sanitizedOrigin);
        if (originValidation.IsFailure)
        {
            return originValidation.Error!;
        }

        var sanitizedDestination = destinationFolderName.RemoveDiacritics();
        var destinationValidation = ValidatePath(sanitizedDestination);
        if (destinationValidation.IsFailure)
        {
            return destinationValidation.Error!;
        }

        var originPath = new StringBuilder();
        originPath.Append(rootPath.TrimEnd(_charsToTrim)).Append(Path.DirectorySeparatorChar).Append(sanitizedOrigin);

        if (!Directory.Exists(originPath.ToString()))
        {
            return LibraryLocalStorageError.UnknownFolder;
        }

        var destinationPath = new StringBuilder();
        destinationPath.Append(rootPath.TrimEnd(_charsToTrim)).Append(Path.DirectorySeparatorChar).Append(sanitizedDestination);

        if (Directory.Exists(destinationPath.ToString()))
        {
            return LibraryLocalStorageError.AlreadyExistingFolder;
        }

        Directory.Move(originPath.ToString(), destinationPath.ToString());
        return Result.Success();
    }

    public Result<string> MoveFile(string sourceFilePath, string destinationFolderName)
    {
        if (string.IsNullOrEmpty(rootPath) || string.IsNullOrEmpty(sourceFilePath) || string.IsNullOrEmpty(destinationFolderName))
        {
            return LibraryLocalStorageError.ArgumentNullOrEmpty;
        }

        var sanitizedDestination = destinationFolderName.RemoveDiacritics();
        var destinationValidation = ValidatePath(sanitizedDestination);
        if (destinationValidation.IsFailure)
        {
            return destinationValidation.Error!;
        }

        if (!PathContainment.IsWithin(rootPath, sourceFilePath))
        {
            return LibraryLocalStorageError.InvalidPath;
        }

        if (!File.Exists(sourceFilePath))
        {
            return BooksError.FileNotFound;
        }

        var destinationDirectory = Path.Combine(rootPath.TrimEnd(_charsToTrim), sanitizedDestination);
        var destinationPath = Path.Combine(destinationDirectory, Path.GetFileName(sourceFilePath));
        if (File.Exists(destinationPath))
        {
            return BooksError.FileAlreadyExists;
        }

        try
        {
            Directory.CreateDirectory(destinationDirectory);
            File.Move(sourceFilePath, destinationPath, overwrite: false);
            return destinationPath;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return LibraryLocalStorageError.FileMoveFailed;
        }
    }

    public Result Delete(string folderName)
    {
        Guard.Against.Null(rootPath);
        Guard.Against.Null(folderName);

        if (string.IsNullOrEmpty(rootPath) || string.IsNullOrEmpty(folderName))
        {
            return LibraryLocalStorageError.ArgumentNullOrEmpty;
        }

        var sanitizedFolderName = folderName.RemoveDiacritics();
        var pathValidation = ValidatePath(sanitizedFolderName);
        if (pathValidation.IsFailure)
        {
            return pathValidation.Error!;
        }

        var path = new StringBuilder();
        path.Append(rootPath.TrimEnd(_charsToTrim)).Append(Path.DirectorySeparatorChar).Append(sanitizedFolderName);

        if (!Directory.Exists(path.ToString()))
        {
            return LibraryLocalStorageError.UnknownFolder;
        }

        Directory.Delete(path.ToString(), true);
        return Result.Success();
    }

}

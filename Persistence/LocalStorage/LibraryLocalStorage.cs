using System.Text;
using Ardalis.GuardClauses;
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

    public Result Create(string folderName)
    {
        Guard.Against.Null(rootPath);
        Guard.Against.Null(folderName);

        if (String.IsNullOrEmpty(rootPath) || String.IsNullOrEmpty(folderName))
        {
            return LibraryLocalStorageError.ArgumentNullOrEmpty;
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

        if (String.IsNullOrEmpty(rootPath) || String.IsNullOrEmpty(originFolderName) || String.IsNullOrEmpty(destinationFolderName))
        {
            return LibraryLocalStorageError.ArgumentNullOrEmpty;
        }

        var originPath = new StringBuilder();
        originPath.Append(rootPath.TrimEnd(_charsToTrim)).Append(Path.DirectorySeparatorChar).Append(originFolderName.RemoveDiacritics());

        if (!Directory.Exists(originPath.ToString()))
        {
            return LibraryLocalStorageError.UnknownFolder;
        }

        var destinationPath = new StringBuilder();
        destinationPath.Append(rootPath.TrimEnd(_charsToTrim)).Append(Path.DirectorySeparatorChar).Append(destinationFolderName.RemoveDiacritics());

        if (Directory.Exists(destinationPath.ToString()))
        {
            return LibraryLocalStorageError.AlreadyExistingFolder;
        }

        Directory.Move(originPath.ToString(), destinationPath.ToString());
        return Result.Success();
    }

    public Result Delete(string folderName)
    {
        Guard.Against.Null(rootPath);
        Guard.Against.Null(folderName);

        if (String.IsNullOrEmpty(rootPath) || String.IsNullOrEmpty(folderName))
        {
            return LibraryLocalStorageError.ArgumentNullOrEmpty;
        }

        var path = new StringBuilder();
        path.Append(rootPath.TrimEnd(_charsToTrim)).Append(Path.DirectorySeparatorChar).Append(folderName.RemoveDiacritics());

        if (!Directory.Exists(path.ToString()))
        {
            return LibraryLocalStorageError.UnknownFolder;
        }

        Directory.Delete(path.ToString(), true);
        return Result.Success();
    }

}

using System.Text;
using Ardalis.GuardClauses;
using Domain.Errors;
using Domain.Extensions;
using Domain.Primitives;

namespace Persistence.LocalStorage;
public static class LibraryLocalStorage
{
    private static readonly char[] _charsToTrim = ['/', '\\'];

    public static Result Create(string rootPath, string folderName)
    {
        Guard.Against.Null(rootPath);
        Guard.Against.Null(folderName);

        if(String.IsNullOrEmpty(rootPath) || String.IsNullOrEmpty(folderName))
        {
            return LibraryLocalStorageError.ArgumentNullOrEmpty;
        }

        var path = new StringBuilder();
        path.Append(rootPath.TrimEnd(_charsToTrim)).Append(Path.DirectorySeparatorChar).Append(folderName);

        Directory.CreateDirectory(path.ToString());
        return Result.Success();        
    }

    public static Result Move(string rootPath, string oldFolderName, string newFolderName)
    {
        Guard.Against.Null(rootPath);
        Guard.Against.Null(oldFolderName);
        Guard.Against.Null(newFolderName);

        if (String.IsNullOrEmpty(rootPath) || String.IsNullOrEmpty(oldFolderName) || String.IsNullOrEmpty(newFolderName))
        {
            return LibraryLocalStorageError.ArgumentNullOrEmpty;
        }        

        var oldPath = new StringBuilder();
        oldPath.Append(rootPath.TrimEnd(_charsToTrim)).Append(Path.DirectorySeparatorChar).Append(oldFolderName.RemoveDiacritics());

        if (!Directory.Exists(oldPath.ToString()))
        {
            return LibraryLocalStorageError.UnknownFolder;
        }

        var newPath = new StringBuilder();
        newPath.Append(rootPath.TrimEnd(_charsToTrim)).Append(Path.DirectorySeparatorChar).Append(newFolderName.RemoveDiacritics());

        //TODO : Move to an existing folder

        Directory.Move(oldPath.ToString(), newPath.ToString());
        return Result.Success();
    }

    public static void Delete(string rootPath, string folderName) 
    {
        Guard.Against.Null(rootPath);
        Guard.Against.Null(folderName);

        var path = new StringBuilder();
        path.Append(rootPath.TrimEnd(_charsToTrim)).Append(Path.DirectorySeparatorChar).Append(folderName.RemoveDiacritics());

        Directory.Delete(path.ToString());
    }

}

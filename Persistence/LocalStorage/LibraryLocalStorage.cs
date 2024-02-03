
using System.Text;
using Ardalis.GuardClauses;
using Domain.Extensions;

namespace Persistence.LocalStorage;
public class LibraryLocalStorage
{
    private readonly char[] _charsToTrim = ['/', '\\'];

    public void Create(string rootPath, string folderName)
    {
        Guard.Against.Null(rootPath, folderName);

        var path = new StringBuilder();
        path.Append(rootPath.TrimEnd(_charsToTrim)).Append(Path.DirectorySeparatorChar).Append(folderName.RemoveDiacritics());
        
        Directory.CreateDirectory(path.ToString());
    }

    public void Delete(string rootPath, string folderName) 
    {
        Guard.Against.Null(rootPath, folderName);

        var path = new StringBuilder();
        path.Append(rootPath.TrimEnd(_charsToTrim)).Append(Path.DirectorySeparatorChar).Append(folderName.RemoveDiacritics());

        Directory.Delete(path.ToString());
    }

}

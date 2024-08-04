using Domain.Primitives;

namespace Persistence.LocalStorage;
public interface ILibraryLocalStorage
{
    public string rootPath { get; init; }
    Result Create(string folderName);
    Result Delete(string folderName);
    Result Move(string originFolderName, string destinationFolderName);
}

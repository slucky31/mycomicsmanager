using Domain.Primitives;

namespace Application.Libraries;

public interface ILibraryLocalStorage
{
    string rootPath { get; init; }
    Result Create(string folderName);
    Result Delete(string folderName);
    Result Move(string originFolderName, string destinationFolderName);

    // Moves a book file into the folder of another library and returns its new absolute path.
    Result<string> MoveFile(string sourceFilePath, string destinationFolderName);
}

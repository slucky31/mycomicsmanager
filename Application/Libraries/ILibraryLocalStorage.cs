using Domain.Primitives;

namespace Application.Libraries;

public interface ILibraryLocalStorage
{
    string rootPath { get; init; }
    Result Create(string folderName);
    Result Delete(string folderName);
    Result Move(string originFolderName, string destinationFolderName);

    // The absolute paths of the files of a library folder, subfolders included; empty when it does not exist.
    IReadOnlyList<string> ListFiles(string folderName);

    // Moves a book file into the folder of another library and returns its new absolute path.
    Result<string> MoveFile(string sourceFilePath, string destinationFolderName);

    // Where a file of a library folder is once that folder is moved (Move); null for a file outside of it.
    string? PathAfterMove(string filePath, string originFolderName, string destinationFolderName);
}

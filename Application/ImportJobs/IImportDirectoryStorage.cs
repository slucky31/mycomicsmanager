using Domain.Primitives;

namespace Application.ImportJobs;

public interface IImportDirectoryStorage
{
    Result EnsureExists(string directoryName);
    Result Delete(string directoryName);
    Result Move(string originDirectoryName, string destinationDirectoryName);
    Result DeleteOriginalFile(string absoluteFilePath);
    Result MoveOriginalFileToError(string absoluteFilePath);

    // Absolute path of a file not yet present in the import directory (a suffix is added if the name is taken).
    Result<string> GetAvailableFilePath(string directoryName, string fileName);

    // Moves a file from outside into the import directory: copied as "{destination}.part" (ignored by the
    // watcher), then renamed in place so the watcher never sees a partial file.
    Result DepositFile(string sourceFilePath, string destinationFilePath);
}

using Domain.Primitives;

namespace Application.ImportJobs;

public interface IImportDirectoryStorage
{
    Result EnsureExists(string directoryName);
    Result Delete(string directoryName);
    Result Move(string originDirectoryName, string destinationDirectoryName);
    Result DeleteOriginalFile(string absoluteFilePath);
    Result MoveOriginalFileToError(string absoluteFilePath);
}

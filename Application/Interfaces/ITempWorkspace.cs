using Domain.Primitives;

namespace Application.Interfaces;

public interface ITempWorkspace
{
    bool HasFreeSpace(long requiredBytes);
    (string TempDir, string RawDir, string ConvertedDir) CreateScratch(Guid jobId);
    IReadOnlyList<string> GetWebpFiles(string directory);
    Result<string> MoveToLibrary(string sourcePath, string libraryRelativePath, string fileName);
    void TryDeleteFile(string path);
    void CleanupDirectory(string dir);
}

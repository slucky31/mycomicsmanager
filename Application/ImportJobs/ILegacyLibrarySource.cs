using Domain.Primitives;

namespace Application.ImportJobs;

// The folder of the old library, emptied into the import directory of the "Imported" library.
public interface ILegacyLibrarySource
{
    // The first files of the folder in name order (absolute paths), and the number of files it holds.
    Result<LegacyLibraryBatch> GetFirstFiles(int count);

    // The number of files still in the folder, 0 once it is emptied (#1292).
    Result<int> CountFiles();
}

public sealed record LegacyLibraryBatch(IReadOnlyList<string> Files, int TotalCount);

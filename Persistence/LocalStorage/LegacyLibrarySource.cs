using Application.ImportJobs;
using Domain.Errors;
using Domain.Primitives;

namespace Persistence.LocalStorage;

internal sealed class LegacyLibrarySource(string directory) : ILegacyLibrarySource
{
    // The old library, moved on the host into the folder mounted as /data (one-off migration, #1174).
    public const string LegacyDirectory = "/data/legacy";

    public Result<LegacyLibraryBatch> GetFirstFiles(int count)
    {
        if (!Directory.Exists(directory))
        {
            return LegacyMigrationError.SourceNotFound;
        }

        // Ordinal: the byte order of "sort" in the original script.
        var files = Directory.EnumerateFiles(directory)
            .Order(StringComparer.Ordinal)
            .ToList();
        return new LegacyLibraryBatch([.. files.Take(Math.Max(count, 0))], files.Count);
    }
}

using Application.ImportJobs;
using Domain.Errors;
using Domain.Primitives;

namespace Persistence.LocalStorage;

internal sealed class LegacyLibrarySource(string directory) : ILegacyLibrarySource
{
    // The old library, mounted as its own volume in the container (one-off migration, #1174).
    public const string LegacyDirectory = "/home/app/mcm/legacy";

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
        // Docker creates an empty folder when the host path of the volume does not exist.
        if (files.Count == 0)
        {
            return LegacyMigrationError.SourceEmpty;
        }

        return new LegacyLibraryBatch([.. files.Take(Math.Max(count, 0))], files.Count);
    }
}

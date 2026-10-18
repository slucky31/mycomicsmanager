using Application.ImportJobs;
using Domain.Errors;
using Domain.Primitives;
using Microsoft.Extensions.Options;

namespace Persistence.LocalStorage;

internal sealed class LegacyLibrarySource(IOptions<ImportSettings> settings) : ILegacyLibrarySource
{
    private readonly string _directory = settings.Value.LegacyLibraryDirectory;

    public Result<LegacyLibraryBatch> GetFirstFiles(int count)
    {
        if (string.IsNullOrWhiteSpace(_directory))
        {
            return LegacyMigrationError.NotConfigured;
        }

        if (!Directory.Exists(_directory))
        {
            return LegacyMigrationError.SourceNotFound;
        }

        // Ordinal: the byte order of "sort" in the original script.
        var files = Directory.EnumerateFiles(_directory)
            .Order(StringComparer.Ordinal)
            .ToList();
        return new LegacyLibraryBatch([.. files.Take(Math.Max(count, 0))], files.Count);
    }
}

using Application.Libraries;
using Domain.Errors;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.ImportJobs.MigrateLegacy;

// The "Imported" library the files go to and the first files of the old library: checked when the migration is
// started from the Settings page, then again by the background job, since the folders may have changed in between.
internal sealed record LegacyMigrationPlan(Library Library, LegacyLibraryBatch Batch)
{
    public const int MaxCount = 200;

    public static async Task<Result<LegacyMigrationPlan>> CreateAsync(
        int count,
        Guid userId,
        ILibraryReadService libraryReadService,
        ILegacyLibrarySource legacyLibrarySource,
        CancellationToken cancellationToken)
    {
        if (count is < 1 or > MaxCount || userId == Guid.Empty)
        {
            return LegacyMigrationError.BadRequest;
        }

        var library = await libraryReadService.GetByNameAsync(LibraryConstants.ImportedName, userId, cancellationToken);
        if (library is null || library.BookType != LibraryBookType.Digital)
        {
            return LegacyMigrationError.ImportedLibraryNotFound;
        }

        var batch = legacyLibrarySource.GetFirstFiles(count);
        return batch.IsFailure ? batch.Error! : new LegacyMigrationPlan(library, batch.Value!);
    }
}

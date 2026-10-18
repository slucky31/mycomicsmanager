using Application.ImportJobs.MigrateLegacy;
using Domain.Primitives;

namespace Web.Services;

public interface ILegacyMigrationService
{
    // Moves the first files of the old library into the import directory of the current user's "Imported" library.
    Task<Result<MigrateLegacyBooksResult>> MigrateAsync(int count, CancellationToken cancellationToken = default);
}

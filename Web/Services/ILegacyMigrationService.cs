using Domain.Primitives;

namespace Web.Services;

public interface ILegacyMigrationService
{
    /// <summary>Moves, in the background, the first files of the old library into the import directory of the current user's "Imported" library.</summary>
    /// <returns>The number of files to move.</returns>
    Task<Result<int>> StartAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>Counts the files still in the old library, waiting to be moved to the import directory.</summary>
    Task<Result<int>> CountRemainingAsync(CancellationToken cancellationToken = default);
}

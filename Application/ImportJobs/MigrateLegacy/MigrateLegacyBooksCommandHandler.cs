using Application.Abstractions.Messaging;
using Application.Libraries;
using Ardalis.GuardClauses;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Application.ImportJobs.MigrateLegacy;

// One-off migration of the old library: moves its first files (name order) into the import directory of the
// "Imported" library, where the file watcher imports them like any other deposit.
public sealed class MigrateLegacyBooksCommandHandler(
    ILegacyLibrarySource legacyLibrarySource,
    ILibraryReadService libraryReadService,
    IImportDirectoryStorage importDirectoryStorage,
    ILogger<MigrateLegacyBooksCommandHandler> logger) : ICommandHandler<MigrateLegacyBooksCommand, MigrateLegacyBooksResult>
{
    public const int MaxCount = LegacyMigrationPlan.MaxCount;

    public async Task<Result<MigrateLegacyBooksResult>> Handle(MigrateLegacyBooksCommand command, CancellationToken cancellationToken)
    {
        Guard.Against.Null(command);
        var plan = await LegacyMigrationPlan.CreateAsync(command.Count, command.UserId, libraryReadService, legacyLibrarySource, cancellationToken);
        if (plan.IsFailure)
        {
            return plan.Error!;
        }

        var (library, batch) = plan.Value!;
        var ensured = importDirectoryStorage.EnsureExists(library.ImportDirectoryName);
        if (ensured.IsFailure)
        {
            return ensured.Error!;
        }

        int moved = 0, skipped = 0, failed = 0;
        foreach (var file in batch.Files)
        {
            switch (Deposit(library.ImportDirectoryName, file))
            {
                case DepositOutcome.Moved:
                    moved++;
                    break;
                case DepositOutcome.Skipped:
                    skipped++;
                    break;
                default:
                    failed++;
                    break;
            }
        }

        logger.LogInformation("Legacy migration: {Moved} file(s) moved, {Skipped} skipped, {Failed} failed", moved, skipped, failed);
        return new MigrateLegacyBooksResult(moved, skipped, failed, batch.TotalCount - moved);
    }

    private DepositOutcome Deposit(string importDirectoryName, string file)
    {
        var fileName = Path.GetFileName(file);
        var destination = importDirectoryStorage.GetAvailableFilePath(importDirectoryName, fileName);
        if (destination.IsFailure)
        {
            logger.LogWarning("Legacy migration: {FileName} not moved: {Error}", fileName, destination.Error!.Description);
            return DepositOutcome.Failed;
        }

        // Another name was chosen: a file with this name is already there (same as "mv -n").
        if (!string.Equals(Path.GetFileName(destination.Value), fileName, StringComparison.Ordinal))
        {
            return DepositOutcome.Skipped;
        }

        var deposited = importDirectoryStorage.DepositFile(file, destination.Value!);
        if (deposited.IsFailure)
        {
            logger.LogWarning("Legacy migration: {FileName} not moved: {Error}", fileName, deposited.Error!.Description);
            return DepositOutcome.Failed;
        }

        return DepositOutcome.Moved;
    }

    private enum DepositOutcome
    {
        Moved,
        Skipped,
        Failed
    }
}

using Application.Abstractions.Messaging;

namespace Application.ImportJobs.MigrateLegacy;

// Returns the number of files of the old library that will be moved into the import directory in the background (#1204).
public record StartLegacyMigrationCommand(int Count, Guid UserId) : ICommand<int>;

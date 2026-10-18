using Application.Abstractions.Messaging;

namespace Application.ImportJobs.MigrateLegacy;

public record MigrateLegacyBooksCommand(int Count, Guid UserId) : ICommand<MigrateLegacyBooksResult>;

// Skipped: a file with the same name is already waiting in the import directory (left in the old library).
public sealed record MigrateLegacyBooksResult(int Moved, int Skipped, int Failed, int Remaining);

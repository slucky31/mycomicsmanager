using Application.Abstractions.Messaging;

namespace Application.FeedImports.Refresh;

// Follows the ImportJobs of the Downloaded decisions: Imported once the book exists, Failed if the import failed.
public record RefreshFeedImportStatusesCommand(Guid UserId) : ICommand;

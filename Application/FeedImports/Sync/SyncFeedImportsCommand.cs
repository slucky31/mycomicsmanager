using Application.Abstractions.Messaging;

namespace Application.FeedImports.Sync;

public record SyncFeedImportsCommand(Guid UserId) : ICommand<SyncFeedImportsResult>;

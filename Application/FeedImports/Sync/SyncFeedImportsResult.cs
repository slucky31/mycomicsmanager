namespace Application.FeedImports.Sync;

public sealed record SyncFeedImportsResult(int StarredEntries, int Created, int AlreadyKnown, int Rejected, int UnstarFailures);

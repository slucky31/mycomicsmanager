using Application.Abstractions.Messaging;

namespace Application.Books.ComicInfoSync;

// Run in the background, for the user who started it.
public record SyncComicInfoCommand(Guid UserId) : ICommand<ComicInfoSyncSummary>;

public sealed record ComicInfoSyncSummary(int Updated, int Failed);

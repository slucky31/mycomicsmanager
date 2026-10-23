using Application.Abstractions.Messaging;

namespace Application.Books.ArchiveRefresh;

// Run in the background, for the user who started it.
public record RefreshFromArchiveCommand(Guid UserId) : ICommand<ArchiveRefreshSummary>;

public sealed record ArchiveRefreshSummary(int Updated, int Unchanged, int Failed);

using Application.Abstractions.Messaging;

namespace Application.FeedImports.HostStatus;

// Reads the Debrid-Link status page and keeps the hosts whose state changed (HostStatusChange).
public record RecordHostStatusesCommand : ICommand<RecordHostStatusesResult>;

// CameBackUp: hosts that were down at the previous reading and are up now.
public sealed record RecordHostStatusesResult(int Changes, IReadOnlyList<string> CameBackUp, int Deleted);

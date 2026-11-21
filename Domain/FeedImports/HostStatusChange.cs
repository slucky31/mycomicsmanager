using Domain.Primitives;

namespace Domain.FeedImports;

// A host of the Debrid-Link status page went up or down (or was seen for the first time).
// Only the changes are kept, so the latest one of a host tells since when it is in its state.
public class HostStatusChange : Entity<Guid>
{
    // Debrid-Link name of the host ("1fichier", "ddl"...), stable while its domains change.
    public string Host { get; private set; } = string.Empty;

    public bool IsUp { get; private set; }

    public DateTime ChangedAt { get; private set; }

    protected HostStatusChange() { }

    public static Result<HostStatusChange> Create(string host, bool isUp, TimeProvider? timeProvider = null)
    {
        if (string.IsNullOrWhiteSpace(host) || host.Trim().Length > FeedImportConstants.MaxHostLength)
        {
            return FeedImportError.BadRequest;
        }

        return new HostStatusChange
        {
            Id = Guid.CreateVersion7(),
            Host = host.Trim(),
            IsUp = isUp,
            ChangedAt = Clock.UtcNow(timeProvider)
        };
    }
}

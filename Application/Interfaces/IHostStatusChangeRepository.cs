using Domain.FeedImports;

namespace Application.Interfaces;

public interface IHostStatusChangeRepository
{
    void Add(HostStatusChange change);

    // The latest change of each host, by host name.
    Task<IReadOnlyDictionary<string, HostStatusChange>> GetLatestPerHostAsync(CancellationToken ct = default);

    // The changes since the date, newest first.
    Task<IReadOnlyList<HostStatusChange>> ListSinceAsync(DateTime since, CancellationToken ct = default);

    // Deletes the changes older than the cutoff, except the latest one of each host. Returns the number deleted.
    Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);
}

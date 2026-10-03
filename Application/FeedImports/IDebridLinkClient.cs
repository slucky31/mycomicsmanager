using Domain.Primitives;

namespace Application.FeedImports;

#pragma warning disable CA1054 // URL comes from the decision links (JSON)
public interface IDebridLinkClient
{
    // Domains Debrid-Link can unlock (GET /downloader/domains).
    Task<Result<IReadOnlyList<string>>> GetSupportedDomainsAsync(CancellationToken cancellationToken = default);

    // Unlocks a hoster link (POST /downloader/add) and returns the direct download link.
    Task<Result<DebridLinkFile>> UnlockAsync(string url, CancellationToken cancellationToken = default);
}
#pragma warning restore CA1054

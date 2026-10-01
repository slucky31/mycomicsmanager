using Domain.Primitives;

namespace Application.FeedImports;

public interface IMinifluxClient
{
    Task<Result<IReadOnlyList<MinifluxCategory>>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<MinifluxEntry>>> GetStarredEntriesAsync(long categoryId, CancellationToken cancellationToken = default);

    // Miniflux only exposes a toggle: implementations must check the entry is still starred before toggling.
    Task<Result> UnstarAsync(long entryId, CancellationToken cancellationToken = default);
}

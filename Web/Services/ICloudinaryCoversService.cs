using Application.Books.Covers;
using Domain.Primitives;

namespace Web.Services;

// The covers of Cloudinary of every user (#1225), managed from Settings › Administration.
public interface ICloudinaryCoversService
{
    /// <summary>The number of books whose cover is still an image shared under its ISBN or in digital-covers.</summary>
    Task<Result<int>> CountToMoveAsync(CancellationToken cancellationToken = default);

    /// <summary>Copies, in the background, the cover of every book to its own image.</summary>
    /// <returns>The number of covers to copy.</returns>
    Task<Result<int>> StartMoveAsync(CancellationToken cancellationToken = default);

    Task<Result<UnusedCovers>> FindUnusedAsync(CancellationToken cancellationToken = default);

    Task<Result<DeletedCovers>> DeleteUnusedAsync(IReadOnlyList<string> publicIds, CancellationToken cancellationToken = default);
}

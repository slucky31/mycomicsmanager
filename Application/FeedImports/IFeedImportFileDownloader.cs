using Domain.Primitives;

namespace Application.FeedImports;

public interface IFeedImportFileDownloader
{
    // Streams the file into Import:TempDirectory; fails (and deletes the partial file) beyond maxBytes.
    Task<Result<DownloadedFile>> DownloadAsync(Uri downloadUrl, long maxBytes, CancellationToken cancellationToken = default);

    void Discard(string tempFilePath);
}

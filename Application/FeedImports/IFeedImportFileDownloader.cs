using Domain.Primitives;

namespace Application.FeedImports;

public interface IFeedImportFileDownloader
{
    // Streams the file into Import:TempDirectory; fails (and deletes the partial file) beyond maxBytes,
    // when the server stops sending data (FeedImport:DownloadIdleTimeout) or beyond FeedImport:DownloadTimeout.
    Task<Result<DownloadedFile>> DownloadAsync(Uri downloadUrl, long maxBytes, CancellationToken cancellationToken = default);

    void Discard(string tempFilePath);

    // Deletes the partial downloads left by a killed process (not written for olderThan); returns how many.
    int DiscardStale(TimeSpan olderThan);
}

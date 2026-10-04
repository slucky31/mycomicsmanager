using Application.FeedImports;
using Application.ImportJobs;
using Domain.FeedImports;
using Domain.Primitives;
using Microsoft.Extensions.Options;

namespace Web.Infrastructure;

// Streams an unlocked file into Import:TempDirectory, never into the watched import directory.
internal sealed class FeedImportFileDownloader(HttpClient httpClient, IOptions<ImportSettings> importSettings, ILogger<FeedImportFileDownloader> logger) : IFeedImportFileDownloader
{
    private const int BufferSize = 81920;

    public async Task<Result<DownloadedFile>> DownloadAsync(Uri downloadUrl, long maxBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(downloadUrl);

        var directory = Path.Combine(importSettings.Value.TempDirectory, "feed-imports");
        Directory.CreateDirectory(directory);
        var tempPath = Path.Combine(directory, $"{Guid.CreateVersion7():N}.download");
        try
        {
            var result = await DownloadToAsync(downloadUrl, tempPath, maxBytes, cancellationToken);
            if (result.IsFailure)
            {
                Discard(tempPath);
            }

            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException
                                   || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning(ex, "Download from {Host} failed", downloadUrl.Host);
            Discard(tempPath);
            return FeedImportError.DownloadFailed;
        }
        catch (OperationCanceledException)
        {
            Discard(tempPath);
            throw;
        }
    }

    public void Discard(string tempFilePath)
    {
        try
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Temporary download {Path} could not be deleted", tempFilePath);
        }
    }

    private async Task<Result<DownloadedFile>> DownloadToAsync(Uri downloadUrl, string tempPath, long maxBytes, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Download from {Host} returned {StatusCode}", downloadUrl.Host, (int)response.StatusCode);
            return new TError(FeedImportError.DownloadFailed.Code, $"{FeedImportError.DownloadFailed.Description} (HTTP {(int)response.StatusCode})");
        }

        if (response.Content.Headers.ContentLength > maxBytes)
        {
            return FeedImportError.FileTooLarge;
        }

        // Hard cap while copying: a missing or lying Content-Length cannot fill the disk.
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
        var buffer = new byte[BufferSize];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                return FeedImportError.FileTooLarge;
            }

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        if (total == 0)
        {
            return new TError(FeedImportError.DownloadFailed.Code, $"{FeedImportError.DownloadFailed.Description} (fichier vide)");
        }

        return new DownloadedFile(tempPath, total);
    }
}

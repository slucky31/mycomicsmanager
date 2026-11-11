using System.Globalization;
using Application.FeedImports;
using Application.ImportJobs;
using Domain.FeedImports;
using Domain.Primitives;
using Microsoft.Extensions.Options;

namespace Web.Infrastructure;

// Streams an unlocked file into Import:TempDirectory, never into the watched import directory.
internal sealed class FeedImportFileDownloader(
    HttpClient httpClient,
    IOptions<ImportSettings> importSettings,
    IOptions<FeedImportSettings> feedImportSettings,
    ILogger<FeedImportFileDownloader> logger) : IFeedImportFileDownloader
{
    private const int BufferSize = 81920;
    private const string PartialFilePattern = "*.download";

    private string DownloadDirectory => Path.Combine(importSettings.Value.TempDirectory, "feed-imports");

    public async Task<Result<DownloadedFile>> DownloadAsync(Uri downloadUrl, long maxBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(downloadUrl);

        Directory.CreateDirectory(DownloadDirectory);
        var tempPath = Path.Combine(DownloadDirectory, $"{Guid.CreateVersion7():N}.download");
        try
        {
            var result = await DownloadWithTimeoutsAsync(downloadUrl, tempPath, maxBytes, cancellationToken);
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

    public int DiscardStale(TimeSpan olderThan)
    {
        if (!Directory.Exists(DownloadDirectory))
        {
            return 0;
        }

        var limit = DateTime.UtcNow - olderThan;
        var stale = new DirectoryInfo(DownloadDirectory).EnumerateFiles(PartialFilePattern)
            .Where(file => file.LastWriteTimeUtc < limit)
            .ToList();
        foreach (var file in stale)
        {
            logger.LogWarning("Partial download {Path} ({SizeBytes} bytes, last written {LastWriteUtc:u}) left by an interrupted download: deleted",
                file.FullName, file.Length, file.LastWriteTimeUtc);
            Discard(file.FullName);
        }

        return stale.Count;
    }

    // HttpClient.Timeout does not cover the body (ResponseHeadersRead): a server that keeps the connection open
    // without sending anything would block the job forever. The idle delay is re-armed before every read.
    private async Task<Result<DownloadedFile>> DownloadWithTimeoutsAsync(Uri downloadUrl, string tempPath, long maxBytes, CancellationToken cancellationToken)
    {
        var settings = feedImportSettings.Value;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(settings.DownloadTimeout);
        using var idle = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
        try
        {
            return await DownloadToAsync(downloadUrl, tempPath, maxBytes, new IdleTimeout(idle, settings.DownloadIdleTimeout));
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && idle.IsCancellationRequested)
        {
            var reason = deadline.IsCancellationRequested
                ? $"took longer than {Format(settings.DownloadTimeout)}"
                : $"no data received for {Format(settings.DownloadIdleTimeout)}";
            logger.LogWarning(ex, "Download from {Host} aborted: {Reason}", downloadUrl.Host, reason);
            return new TError(FeedImportError.DownloadFailed.Code, $"{FeedImportError.DownloadFailed.Description} ({reason})");
        }
    }

    private async Task<Result<DownloadedFile>> DownloadToAsync(Uri downloadUrl, string tempPath, long maxBytes, IdleTimeout idle)
    {
        using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, idle.Rearm());
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
        await using var source = await response.Content.ReadAsStreamAsync(idle.Rearm());
        await using var target = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
        var buffer = new byte[BufferSize];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, idle.Rearm())) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                return FeedImportError.FileTooLarge;
            }

            await target.WriteAsync(buffer.AsMemory(0, read), idle.Token);
        }

        if (total == 0)
        {
            return new TError(FeedImportError.DownloadFailed.Code, $"{FeedImportError.DownloadFailed.Description} (empty file)");
        }

        return new DownloadedFile(tempPath, total);
    }

    internal static string Format(TimeSpan delay) =>
        delay.TotalMinutes >= 1 && delay.Seconds == 0 && delay.Milliseconds == 0
            ? string.Create(CultureInfo.InvariantCulture, $"{delay.TotalMinutes:0} min")
            : string.Create(CultureInfo.InvariantCulture, $"{delay.TotalSeconds:0.###} s");

    private readonly record struct IdleTimeout(CancellationTokenSource Source, TimeSpan Delay)
    {
        public CancellationToken Token => Source.Token;

        // CancelAfter restarts the countdown: the delay is counted from the last data received.
        public CancellationToken Rearm()
        {
            Source.CancelAfter(Delay);
            return Source.Token;
        }
    }
}

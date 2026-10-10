using Application.Books.IsbnScan;
using Application.Helpers;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Persistence.Services;

public sealed class IsbnPageScanner(
    IPageTextRecognizer recognizer,
    IComicPageReader pageReader,
    IOptions<IsbnOcrSettings> options,
    ILogger<IsbnPageScanner> logger) : IIsbnPageScanner
{
    private readonly IsbnOcrSettings _settings = options.Value;

    public Task<IsbnScanResult> ScanPagesAsync(IReadOnlyList<string> pageFiles, CancellationToken ct = default) =>
        ScanAsync(pageFiles.Count, (index, token) => ReadFileAsync(pageFiles[index], token), ct);

    public async Task<IsbnScanResult> ScanArchiveAsync(string archivePath, CancellationToken ct = default)
    {
        if (!_settings.Enabled)
        {
            return IsbnScanResult.NotScanned;
        }

        var pageCount = await pageReader.CountPagesAsync(archivePath, ct);
        if (pageCount.IsFailure)
        {
            logger.LogWarning("Archive {Path} cannot be scanned for an ISBN: {Error}", archivePath, pageCount.Error?.Code);
            return IsbnScanResult.NotScanned;
        }

        return await ScanAsync(pageCount.Value, (index, token) => ReadArchivePageAsync(archivePath, index, token), ct);
    }

    private async Task<IsbnScanResult> ScanAsync(
        int pageCount,
        Func<int, CancellationToken, Task<ReadOnlyMemory<byte>?>> readPage,
        CancellationToken ct)
    {
        if (!_settings.Enabled)
        {
            return IsbnScanResult.NotScanned;
        }

        foreach (var pageIndex in IsbnScanPages.GetScanOrder(pageCount, _settings.PagesFromEachEnd))
        {
            var image = await readPage(pageIndex, ct);
            if (image is null)
            {
                continue;
            }

            var text = await recognizer.RecognizeAsync(image.Value, ct);
            if (text.IsFailure && text.Error == OcrError.Unavailable)
            {
                // No page can be read: the book stays to be scanned once the engine is installed.
                return IsbnScanResult.NotScanned;
            }

            var isbns = TextIsbnExtractor.ExtractAll(text.Value);
            if (isbns.Count > 0)
            {
                return new IsbnScanResult(true, isbns);
            }
        }

        return new IsbnScanResult(true, []);
    }

    private async Task<ReadOnlyMemory<byte>?> ReadArchivePageAsync(string archivePath, int pageIndex, CancellationToken ct)
    {
        var page = await pageReader.ReadPageAsync(archivePath, pageIndex, ct);
        if (page.IsFailure)
        {
            return null;
        }

        return page.Value!.Content;
    }

    private async Task<ReadOnlyMemory<byte>?> ReadFileAsync(string path, CancellationToken ct)
    {
        try
        {
            return await File.ReadAllBytesAsync(path, ct);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Page {Path} cannot be read for the ISBN scan", path);
            return null;
        }
    }
}

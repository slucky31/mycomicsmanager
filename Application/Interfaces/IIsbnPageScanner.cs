namespace Application.Interfaces;

/// <summary>
/// Outcome of a scan. <see cref="Completed"/> is false when the pages could not be scanned at all
/// (OCR disabled or unavailable, archive unreadable): the book can then be scanned again later.
/// </summary>
public sealed record IsbnScanResult(bool Completed, IReadOnlyList<string> Isbns)
{
    public static IsbnScanResult NotScanned { get; } = new(false, []);
}

/// <summary>
/// Reads the first and last pages of a book (most likely first) until one shows an ISBN.
/// </summary>
public interface IIsbnPageScanner
{
    /// <param name="pageFiles">The page images of a book being imported, in reading order.</param>
    /// <param name="ct">Cancels the scan.</param>
    Task<IsbnScanResult> ScanPagesAsync(IReadOnlyList<string> pageFiles, CancellationToken ct = default);

    Task<IsbnScanResult> ScanArchiveAsync(string archivePath, CancellationToken ct = default);
}

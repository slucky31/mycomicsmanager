namespace Persistence.Services;

/// <summary>
/// What one import may extract (#1370): a file of a few kilobytes must not fill the disk of the server.
/// </summary>
internal sealed record ExtractionLimits(int MaxEntries, long MaxBytes, Func<string, long> AvailableBytes)
{
    public static ExtractionLimits Default { get; } = new(5_000, 5L * 1024 * 1024 * 1024, DiskSpace.AvailableBytes);

    // The raw bytes of an image are its bytes in the PDF: only an image shown on several pages can add up to
    // more than the file. Four times its size leaves room for a logo repeated on every page.
    public const int PdfSizeFactor = 4;
}

internal static class DiskSpace
{
    // Measured on the folder itself, which may be a volume of its own (/data/temp), not on the root of its path.
    // Unknown (long.MaxValue) when the system cannot tell: the import goes on, as before this check.
    public static long AvailableBytes(string directory)
    {
        try
        {
            var existing = Path.GetFullPath(directory);
            while (!Directory.Exists(existing) && Path.GetDirectoryName(existing) is { } parent)
            {
                existing = parent;
            }

            return new DriveInfo(existing).AvailableFreeSpace;
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            return long.MaxValue;
        }
    }
}

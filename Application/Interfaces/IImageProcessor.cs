using Domain.Primitives;

namespace Application.Interfaces;

public record ImageProcessingResult(
    int ProcessedCount,
    int SkippedCount,
    bool AllAlreadyWebp);

public record ImageConversionProgress(int ConvertedCount, int TotalCount);

public interface IImageProcessor
{
    /// <summary>
    /// Converts all images in sourceDirectory to WebP, resizes them, and writes them
    /// sequentially (page-001.webp, page-002.webp, …) into destinationDirectory.
    /// If all source files are already .webp the step is skipped.
    /// <paramref name="onProgressAsync"/> is called after each image is converted and can be
    /// used to persist progress. It receives the current count and total count.
    /// </summary>
    Task<Result<ImageProcessingResult>> ProcessImagesAsync(
        string sourceDirectory,
        string destinationDirectory,
        int targetWidth = 1400,
        Func<ImageConversionProgress, Task>? onProgressAsync = null,
        CancellationToken ct = default);
}

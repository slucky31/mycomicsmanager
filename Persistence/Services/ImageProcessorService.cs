using Application.Interfaces;
using Domain.Errors;
using Domain.Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Persistence.Services;

public class ImageProcessorService : IImageProcessor
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<ImageProcessorService>();

    private static readonly HashSet<string> s_inputExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpeg", ".jpg", ".png", ".gif", ".webp" };

    public async Task<Result<ImageProcessingResult>> ProcessImagesAsync(
        string sourceDirectory,
        string destinationDirectory,
        int targetWidth = 1400,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return FileProcessingError.InvalidPath;
        }

        var imageFiles = GetSortedImageFiles(sourceDirectory);

        if (imageFiles.Count == 0)
        {
            return new ImageProcessingResult(0, 0, false);
        }

        if (AllFilesAreWebp(imageFiles))
        {
            Log.Information("All {Count} files are already WebP, skipping conversion", imageFiles.Count);
            return new ImageProcessingResult(0, imageFiles.Count, true);
        }

        Directory.CreateDirectory(destinationDirectory);
        await ProcessAllImagesAsync(imageFiles, destinationDirectory, targetWidth, ct);
        CopyComicInfoXml(sourceDirectory, destinationDirectory);

        Log.Information("Processed {Count} images to WebP", imageFiles.Count);
        return new ImageProcessingResult(imageFiles.Count, 0, false);
    }

    private static List<string> GetSortedImageFiles(string sourceDirectory) =>
        Directory.GetFiles(sourceDirectory)
            .Where(f => s_inputExtensions.Contains(Path.GetExtension(f)))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static bool AllFilesAreWebp(List<string> files) =>
        files.All(f => string.Equals(Path.GetExtension(f), ".webp", StringComparison.OrdinalIgnoreCase));

    private static async Task ProcessAllImagesAsync(
        List<string> imageFiles,
        string destinationDirectory,
        int targetWidth,
        CancellationToken ct)
    {
        for (var i = 0; i < imageFiles.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var outputPath = Path.Combine(destinationDirectory, $"page-{i + 1:D3}.webp");
            await ConvertToWebpAsync(imageFiles[i], outputPath, targetWidth, ct);
        }
    }

    private static async Task ConvertToWebpAsync(
        string sourcePath,
        string outputPath,
        int targetWidth,
        CancellationToken ct)
    {
        using var image = await Image.LoadAsync(sourcePath, ct);
        var effectiveWidth = IsDoublePage(image) ? targetWidth * 2 : targetWidth;
        var targetHeight = (int)Math.Round((double)image.Height * effectiveWidth / image.Width);

        image.Mutate(x => x.Resize(effectiveWidth, targetHeight));
        await image.SaveAsWebpAsync(outputPath, new WebpEncoder(), ct);
    }

    private static bool IsDoublePage(Image image) => image.Width > image.Height;

    private static void CopyComicInfoXml(string sourceDirectory, string destinationDirectory)
    {
        var xmlSource = Path.Combine(sourceDirectory, "ComicInfo.xml");
        if (!File.Exists(xmlSource))
        {
            return;
        }

        var xmlDest = Path.Combine(destinationDirectory, "ComicInfo.xml");
        if (!xmlSource.Equals(xmlDest, StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(xmlSource, xmlDest, overwrite: true);
        }
    }
}

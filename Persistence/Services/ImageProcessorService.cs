using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Persistence.Services;

// ImageSharp implementation (current one): the whole page is decoded before being resized.
public class ImageProcessorService(ILogger<ImageProcessorService> logger) : WebpImageProcessorBase(logger)
{
    protected override async Task<ImageSize?> IdentifyAsync(string filePath, CancellationToken ct)
    {
        var info = await Image.IdentifyAsync(filePath, ct);
        return info is null ? null : new ImageSize(info.Width, info.Height);
    }

    protected override async Task ConvertToWebpAsync(string sourcePath, string outputPath, int targetWidth, CancellationToken ct)
    {
        using var image = await Image.LoadAsync(sourcePath, ct);
        var size = new ImageSize(image.Width, image.Height);
        EnsureSupportedDimensions(size);

        var effectiveWidth = EffectiveWidth(size, targetWidth);
        var targetHeight = (int)Math.Round((double)image.Height * effectiveWidth / image.Width);

        image.Mutate(x => x.Resize(effectiveWidth, targetHeight));
        await image.SaveAsWebpAsync(outputPath, new WebpEncoder(), ct);
    }
}

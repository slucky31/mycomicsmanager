using Persistence.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Application.UnitTests.Services;

public sealed class SkiaImageProcessorServiceTests()
    : ImageProcessorContractTests(new SkiaImageProcessorService(NullLogger<SkiaImageProcessorService>.Instance))
{
    // "VP8 " is the lossy WebP bitstream, "VP8L" the lossless one.
    private static string WebpChunk(string path) => System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(path), 12, 4);

    [Fact]
    public async Task ProcessImagesAsync_Should_EncodePngAsLossyWebp()
    {
        // Arrange
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        using (var image = new Image<Rgba32>(2000, 3000, new Rgba32(200, 30, 30)))
        {
            await image.SaveAsPngAsync(Path.Combine(sourceDir, "page-001.png"), TestContext.Current.CancellationToken);
        }

        // Act
        var result = await Service.ProcessImagesAsync(sourceDir, destDir, 1400, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        WebpChunk(Path.Combine(destDir, "page-001.webp")).Should().Be("VP8 ");
    }

    [Theory]
    [InlineData(1988, 1400, 1)] // 1.4×: a 6/8 decode would soften the lines, full decode then resize
    [InlineData(2797, 1400, 1)] // 1/2 would give 1399px
    [InlineData(2800, 1400, 2)]
    [InlineData(4000, 1400, 2)] // 2.9×: 1/2, not 3/8
    [InlineData(5599, 1400, 4)] // 1/4 rounds up to 1400
    [InlineData(11200, 1400, 8)]
    [InlineData(1000, 1400, 1)] // smaller than the target
    public void DecodeScaleDenominator_Should_KeepOnlyPowerOfTwoScales_WhenSourceIsLargerThanTarget(int sourceWidth, int targetWidth, int expected)
    {
        SkiaImageProcessorService.DecodeScaleDenominator(sourceWidth, targetWidth).Should().Be(expected);
    }

    [Theory]
    [InlineData(5600, 8000)] // 4× the target: decoded at 1/4 scale, no resize step left
    [InlineData(3000, 4500)] // 2.1×: decoded at 1/2 scale, then a final resize
    [InlineData(7000, 5000)] // double page, 2.5× the doubled target
    public async Task ProcessImagesAsync_Should_ReachExactTargetSize_WhenJpegIsDecodedAtReducedScale(int width, int height)
    {
        // Arrange
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        using (var image = new Image<Rgba32>(width, height, new Rgba32(10, 120, 200)))
        {
            await image.SaveAsJpegAsync(Path.Combine(sourceDir, "page-001.jpg"), TestContext.Current.CancellationToken);
        }
        var expectedWidth = width > height ? 2800 : 1400;
        var expectedHeight = (int)Math.Round((double)height * expectedWidth / width);

        // Act
        var result = await Service.ProcessImagesAsync(sourceDir, destDir, 1400, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var info = await Image.IdentifyAsync(Path.Combine(destDir, "page-001.webp"), TestContext.Current.CancellationToken);
        info.Width.Should().Be(expectedWidth);
        info.Height.Should().Be(expectedHeight);
    }
}

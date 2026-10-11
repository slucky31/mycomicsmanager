using Application.Interfaces;
using Domain.Errors;
using Persistence.Services;
using SkiaSharp;

namespace Application.UnitTests.Services;

public sealed class ImageProcessorServiceTests : IDisposable
{
    private readonly string _tempDir;

    public ImageProcessorServiceTests()
    {
        Service = new ImageProcessorService(NullLogger<ImageProcessorService>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    private ImageProcessorService Service { get; }

    private string CreateSourceDir(string name = "source")
    {
        var dir = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static Task CreateJpegAsync(string path, int width = 100, int height = 150) =>
        SaveAsync(path, width, height, SKEncodedImageFormat.Jpeg);

    private static Task CreatePngAsync(string path, int width = 100, int height = 150) =>
        SaveAsync(path, width, height, SKEncodedImageFormat.Png);

    private static Task CreateWebpAsync(string path, int width = 140, int height = 210) =>
        SaveAsync(path, width, height, SKEncodedImageFormat.Webp);

    private static async Task SaveAsync(string path, int width, int height, SKEncodedImageFormat format, SKColor? color = null)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(color ?? SKColors.Black);
        using var data = bitmap.Encode(format, 90);
        await File.WriteAllBytesAsync(path, data.ToArray());
    }

    private readonly record struct PixelSize(int Width, int Height);

    private static PixelSize ReadSize(string path)
    {
        using var codec = SKCodec.Create(path);
        return new PixelSize(codec.Info.Width, codec.Info.Height);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_ConvertJpegToWebp()
    {
        // Arrange
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"));

        // Act
        var result = await Service.ProcessImagesAsync(sourceDir, destDir, 140, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Directory.GetFiles(destDir, "*.webp").Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_ConvertPngToWebp()
    {
        // Arrange
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreatePngAsync(Path.Combine(sourceDir, "page-001.png"));

        // Act
        var result = await Service.ProcessImagesAsync(sourceDir, destDir, 140, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Directory.GetFiles(destDir, "*.webp").Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_SkipConversion_WhenWebpAlreadyAtTargetWidth()
    {
        // Arrange — portrait WebP at exactly targetWidth=140
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateWebpAsync(Path.Combine(sourceDir, "page-001.webp"), width: 140, height: 210);
        await CreateWebpAsync(Path.Combine(sourceDir, "page-002.webp"), width: 140, height: 210);

        // Act
        var result = await Service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 140, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AllAlreadyWebp.Should().BeTrue();
        result.Value.ProcessedCount.Should().Be(0);
        result.Value.SkippedCount.Should().Be(2);
        Directory.GetFiles(destDir, "*.webp").Should().HaveCount(2);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_ResizeToTargetWidth()
    {
        // Arrange
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"), width: 200, height: 300);

        // Act
        await Service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 140, null, TestContext.Current.CancellationToken);

        // Assert
        var outputFile = Directory.GetFiles(destDir, "*.webp").Single();
        var resultImage = ReadSize(outputFile);
        resultImage.Width.Should().Be(140);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_DoubleWidthForDoublePage()
    {
        // Arrange — landscape image (width > height = double page)
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"), width: 300, height: 200);

        // Act
        await Service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 140, null, TestContext.Current.CancellationToken);

        // Assert
        var outputFile = Directory.GetFiles(destDir, "*.webp").Single();
        var resultImage = ReadSize(outputFile);
        resultImage.Width.Should().Be(280);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_PreserveAspectRatio()
    {
        // Arrange — portrait 100x150 → target width 140, expected height 210
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"), width: 100, height: 150);

        // Act
        await Service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 140, null, TestContext.Current.CancellationToken);

        // Assert
        var outputFile = Directory.GetFiles(destDir, "*.webp").Single();
        var resultImage = ReadSize(outputFile);
        resultImage.Width.Should().Be(140);
        resultImage.Height.Should().Be(210);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_ReturnCorrectProcessedCount()
    {
        // Arrange
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"));
        await CreatePngAsync(Path.Combine(sourceDir, "page-002.png"));
        await CreateJpegAsync(Path.Combine(sourceDir, "page-003.jpg"));

        // Act
        var result = await Service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 140, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ProcessedCount.Should().Be(3);
        result.Value.SkippedCount.Should().Be(0);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_ReturnError_WhenDirectoryDoesNotExist()
    {
        // Act
        var result = await Service.ProcessImagesAsync(
            Path.Combine(_tempDir, "nonexistent"),
            Path.Combine(_tempDir, "dest"),
            targetWidth: 140,
            onProgressAsync: null,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileProcessingError.InvalidPath);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_NameFilesSequentially()
    {
        // Arrange
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "img_b.jpg"));
        await CreateJpegAsync(Path.Combine(sourceDir, "img_a.jpg"));
        await CreateJpegAsync(Path.Combine(sourceDir, "img_c.jpg"));

        // Act
        await Service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 140, onProgressAsync: null, TestContext.Current.CancellationToken);

        // Assert
        var files = Directory.GetFiles(destDir, "*.webp").Select(Path.GetFileName).OrderBy(f => f).ToList();
        files.Should().BeEquivalentTo(["page-001.webp", "page-002.webp", "page-003.webp"]);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_CopyComicInfoXml_WhenPresent()
    {
        // Arrange
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"));
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "ComicInfo.xml"), "<ComicInfo/>", TestContext.Current.CancellationToken);

        // Act
        await Service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 140, null, TestContext.Current.CancellationToken);

        // Assert
        File.Exists(Path.Combine(destDir, "ComicInfo.xml")).Should().BeTrue();
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_InvokeProgressCallback_ForEachImage()
    {
        // Arrange
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"));
        await CreateJpegAsync(Path.Combine(sourceDir, "page-002.jpg"));
        await CreateJpegAsync(Path.Combine(sourceDir, "page-003.jpg"));

        var progressReports = new List<ImageConversionProgress>();
        Task OnProgress(ImageConversionProgress p)
        { progressReports.Add(p); return Task.CompletedTask; }

        // Act
        await Service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 140, onProgressAsync: OnProgress, TestContext.Current.CancellationToken);

        // Assert
        progressReports.Should().HaveCount(3);
        progressReports[0].Should().Be(new ImageConversionProgress(1, 3));
        progressReports[1].Should().Be(new ImageConversionProgress(2, 3));
        progressReports[2].Should().Be(new ImageConversionProgress(3, 3));
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_InvokeProgressCallback_EvenForSkippedWebp()
    {
        // Arrange — WebP already at correct width: skipped (copied), but callback still fires
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateWebpAsync(Path.Combine(sourceDir, "page-001.webp"), width: 140, height: 210);

        var progressInvoked = false;
        Task OnProgress(ImageConversionProgress _)
        { progressInvoked = true; return Task.CompletedTask; }

        // Act
        await Service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 140, onProgressAsync: OnProgress, TestContext.Current.CancellationToken);

        // Assert
        progressInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_WorkWithoutProgressCallback()
    {
        // Arrange
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"));

        // Act — passing null explicitly should not throw
        var result = await Service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 140, onProgressAsync: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_ReconvertWebp_WhenWidthDoesNotMatchTarget()
    {
        // Arrange — WebP at 80px, but target is 140px → must be re-converted
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateWebpAsync(Path.Combine(sourceDir, "page-001.webp"), width: 80, height: 120);

        // Act
        var result = await Service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 140, onProgressAsync: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ProcessedCount.Should().Be(1);
        result.Value.SkippedCount.Should().Be(0);
        var outputFile = Directory.GetFiles(destDir, "*.webp").Single();
        var resultImage = ReadSize(outputFile);
        resultImage.Width.Should().Be(140);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_PartiallySkip_WhenMixedArchive()
    {
        // Arrange — 1 JPEG (needs conversion) + 1 WebP at correct width (skip)
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"), width: 100, height: 150);
        await CreateWebpAsync(Path.Combine(sourceDir, "page-002.webp"), width: 140, height: 210);

        // Act
        var result = await Service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 140, onProgressAsync: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ProcessedCount.Should().Be(1);
        result.Value.SkippedCount.Should().Be(1);
        result.Value.AllAlreadyWebp.Should().BeFalse();
        Directory.GetFiles(destDir, "*.webp").Should().HaveCount(2);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_SkipDoublePage_WhenWidthMatchesDoubleTarget()
    {
        // Arrange — landscape WebP at 280px (double page at targetWidth=140) → skip
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateWebpAsync(Path.Combine(sourceDir, "page-001.webp"), width: 280, height: 210);

        // Act
        var result = await Service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 140, onProgressAsync: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.SkippedCount.Should().Be(1);
        result.Value.ProcessedCount.Should().Be(0);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_ReturnFailure_WhenImageIsCorrupt()
    {
        // Arrange — a .jpg file with invalid/non-image content
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        var corruptFile = Path.Combine(sourceDir, "corrupt.jpg");
        await File.WriteAllBytesAsync(corruptFile, [0x00, 0x01, 0x02, 0x03, 0x04], TestContext.Current.CancellationToken);

        // Act
        var result = await Service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 140, onProgressAsync: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("FP503");
        result.Error.Description.Should().Contain("corrupt.jpg");
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_ReturnFailure_WithFirstFailingFilename_WhenMultipleCorruptImages()
    {
        // Arrange — two corrupt images; error should name the first one (alphabetical order)
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await File.WriteAllBytesAsync(Path.Combine(sourceDir, "page-001.jpg"), [0x00, 0x01], TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(sourceDir, "page-002.jpg"), [0x00, 0x01], TestContext.Current.CancellationToken);

        // Act
        var result = await Service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 140, onProgressAsync: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Description.Should().Contain("page-001.jpg");
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_IgnoreDotPrefixedFiles()
    {
        // Arrange — macOS resource fork files like ._IMG0000.png must be skipped
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"));
        await CreatePngAsync(Path.Combine(sourceDir, "._page-001.png"));   // must be ignored
        await File.WriteAllBytesAsync(Path.Combine(sourceDir, "._IMG0000.jpg"), [0x00, 0x01], TestContext.Current.CancellationToken); // corrupt but ignored

        // Act
        var result = await Service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 140, onProgressAsync: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ProcessedCount.Should().Be(1);
        Directory.GetFiles(destDir, "*.webp").Should().HaveCount(1);
    }

    // "VP8 " is the lossy WebP bitstream, "VP8L" the lossless one.
    private static string WebpChunk(string path) => System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(path), 12, 4);

    [Fact]
    public async Task ProcessImagesAsync_Should_EncodePngAsLossyWebp()
    {
        // Arrange
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await SaveAsync(Path.Combine(sourceDir, "page-001.png"), 2000, 3000, SKEncodedImageFormat.Png, new SKColor(200, 30, 30));

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
        ImageProcessorService.DecodeScaleDenominator(sourceWidth, targetWidth).Should().Be(expected);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_ReachExactTargetSize_WhenPngIsReducedInHalvingSteps()
    {
        // Arrange — 600px to 140px: two halving steps (600, 300, 150), then the final resize
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreatePngAsync(Path.Combine(sourceDir, "page-001.png"), width: 600, height: 900);

        // Act
        var result = await Service.ProcessImagesAsync(sourceDir, destDir, 140, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ReadSize(Path.Combine(destDir, "page-001.webp")).Should().Be(new PixelSize(140, 210));
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_ReturnFailure_WhenImageExceedsMaxDimension()
    {
        // Arrange — 8001px is one pixel over the limit that protects the memory of the Raspberry Pi
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreatePngAsync(Path.Combine(sourceDir, "huge.png"), width: 10, height: 8001);

        // Act
        var result = await Service.ProcessImagesAsync(sourceDir, destDir, 140, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("FP503");
        result.Error.Description.Should().Contain("huge.png");
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
        await SaveAsync(Path.Combine(sourceDir, "page-001.jpg"), width, height, SKEncodedImageFormat.Jpeg, new SKColor(10, 120, 200));
        var expectedWidth = width > height ? 2800 : 1400;
        var expectedHeight = (int)Math.Round((double)height * expectedWidth / width);

        // Act
        var result = await Service.ProcessImagesAsync(sourceDir, destDir, 1400, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var info = ReadSize(Path.Combine(destDir, "page-001.webp"));
        info.Width.Should().Be(expectedWidth);
        info.Height.Should().Be(expectedHeight);
    }
}

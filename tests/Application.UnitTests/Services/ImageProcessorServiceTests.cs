using Application.Interfaces;
using Domain.Errors;
using Persistence.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Application.UnitTests.Services;

public sealed class ImageProcessorServiceTests : IDisposable
{
    private readonly ImageProcessorService _service = new();
    private readonly string _tempDir;

    public ImageProcessorServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        GC.SuppressFinalize(this);
    }

    private string CreateSourceDir(string name = "source")
    {
        var dir = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static async Task CreateJpegAsync(string path, int width = 100, int height = 150)
    {
        using var image = new Image<Rgba32>(width, height);
        await image.SaveAsJpegAsync(path);
    }

    private static async Task CreatePngAsync(string path, int width = 100, int height = 150)
    {
        using var image = new Image<Rgba32>(width, height);
        await image.SaveAsPngAsync(path);
    }

    private static async Task CreateWebpAsync(string path, int width = 1400, int height = 2100)
    {
        using var image = new Image<Rgba32>(width, height);
        await image.SaveAsWebpAsync(path);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_ConvertJpegToWebp()
    {
        // Arrange
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"));

        // Act
        var result = await _service.ProcessImagesAsync(sourceDir, destDir, 1400, null, TestContext.Current.CancellationToken);

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
        var result = await _service.ProcessImagesAsync(sourceDir, destDir, 1400, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Directory.GetFiles(destDir, "*.webp").Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_SkipConversion_WhenWebpAlreadyAtTargetWidth()
    {
        // Arrange — portrait WebP at exactly targetWidth=1400
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateWebpAsync(Path.Combine(sourceDir, "page-001.webp"), width: 1400, height: 2100);
        await CreateWebpAsync(Path.Combine(sourceDir, "page-002.webp"), width: 1400, height: 2100);

        // Act
        var result = await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400, null, TestContext.Current.CancellationToken);

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
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"), width: 2000, height: 3000);

        // Act
        await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400, null, TestContext.Current.CancellationToken);

        // Assert
        var outputFile = Directory.GetFiles(destDir, "*.webp").Single();
        using var resultImage = await Image.LoadAsync(outputFile, TestContext.Current.CancellationToken);
        resultImage.Width.Should().Be(1400);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_DoubleWidthForDoublePage()
    {
        // Arrange — landscape image (width > height = double page)
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"), width: 3000, height: 2000);

        // Act
        await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400, null, TestContext.Current.CancellationToken);

        // Assert
        var outputFile = Directory.GetFiles(destDir, "*.webp").Single();
        using var resultImage = await Image.LoadAsync(outputFile, TestContext.Current.CancellationToken);
        resultImage.Width.Should().Be(2800);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_PreserveAspectRatio()
    {
        // Arrange — portrait 1000x1500 → target width 1400, expected height 2100
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"), width: 1000, height: 1500);

        // Act
        await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400, null, TestContext.Current.CancellationToken);

        // Assert
        var outputFile = Directory.GetFiles(destDir, "*.webp").Single();
        using var resultImage = await Image.LoadAsync(outputFile, TestContext.Current.CancellationToken);
        resultImage.Width.Should().Be(1400);
        resultImage.Height.Should().Be(2100);
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
        var result = await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ProcessedCount.Should().Be(3);
        result.Value.SkippedCount.Should().Be(0);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_ReturnError_WhenDirectoryDoesNotExist()
    {
        // Act
        var result = await _service.ProcessImagesAsync(
            Path.Combine(_tempDir, "nonexistent"),
            Path.Combine(_tempDir, "dest"),
            targetWidth: 1400,
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
        await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400, onProgressAsync: null, TestContext.Current.CancellationToken);

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
        await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400, null, TestContext.Current.CancellationToken);

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
        Task OnProgress(ImageConversionProgress p) { progressReports.Add(p); return Task.CompletedTask; }

        // Act
        await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400, onProgressAsync: OnProgress, TestContext.Current.CancellationToken);

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
        await CreateWebpAsync(Path.Combine(sourceDir, "page-001.webp"), width: 1400, height: 2100);

        var progressInvoked = false;
        Task OnProgress(ImageConversionProgress _) { progressInvoked = true; return Task.CompletedTask; }

        // Act
        await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400, onProgressAsync: OnProgress, TestContext.Current.CancellationToken);

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
        var result = await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400, onProgressAsync: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_ReconvertWebp_WhenWidthDoesNotMatchTarget()
    {
        // Arrange — WebP at 800px, but target is 1400px → must be re-converted
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateWebpAsync(Path.Combine(sourceDir, "page-001.webp"), width: 800, height: 1200);

        // Act
        var result = await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400, onProgressAsync: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ProcessedCount.Should().Be(1);
        result.Value.SkippedCount.Should().Be(0);
        var outputFile = Directory.GetFiles(destDir, "*.webp").Single();
        using var resultImage = await Image.LoadAsync(outputFile, TestContext.Current.CancellationToken);
        resultImage.Width.Should().Be(1400);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_PartiallySkip_WhenMixedArchive()
    {
        // Arrange — 1 JPEG (needs conversion) + 1 WebP at correct width (skip)
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"), width: 100, height: 150);
        await CreateWebpAsync(Path.Combine(sourceDir, "page-002.webp"), width: 1400, height: 2100);

        // Act
        var result = await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400, onProgressAsync: null, TestContext.Current.CancellationToken);

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
        // Arrange — landscape WebP at 2800px (double page at targetWidth=1400) → skip
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateWebpAsync(Path.Combine(sourceDir, "page-001.webp"), width: 2800, height: 2100);

        // Act
        var result = await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400, onProgressAsync: null, TestContext.Current.CancellationToken);

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
        var result = await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400, onProgressAsync: null, TestContext.Current.CancellationToken);

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
        var result = await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400, onProgressAsync: null, TestContext.Current.CancellationToken);

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
        var result = await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400, onProgressAsync: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ProcessedCount.Should().Be(1);
        Directory.GetFiles(destDir, "*.webp").Should().HaveCount(1);
    }
}

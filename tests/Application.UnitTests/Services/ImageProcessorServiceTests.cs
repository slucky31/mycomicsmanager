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

    private static void CreateWebpFile(string path)
    {
        // Minimal fake .webp file (just needs the extension for the "all webp" check)
        File.WriteAllBytes(path, [0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50]);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_ConvertJpegToWebp()
    {
        // Arrange
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"));

        // Act
        var result = await _service.ProcessImagesAsync(sourceDir, destDir);

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
        var result = await _service.ProcessImagesAsync(sourceDir, destDir);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Directory.GetFiles(destDir, "*.webp").Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_SkipWhenAllAlreadyWebp()
    {
        // Arrange
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        CreateWebpFile(Path.Combine(sourceDir, "page-001.webp"));
        CreateWebpFile(Path.Combine(sourceDir, "page-002.webp"));

        // Act
        var result = await _service.ProcessImagesAsync(sourceDir, destDir);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AllAlreadyWebp.Should().BeTrue();
        result.Value.ProcessedCount.Should().Be(0);
        result.Value.SkippedCount.Should().Be(2);
    }

    [Fact]
    public async Task ProcessImagesAsync_Should_ResizeToTargetWidth()
    {
        // Arrange
        var sourceDir = CreateSourceDir();
        var destDir = CreateSourceDir("dest");
        await CreateJpegAsync(Path.Combine(sourceDir, "page-001.jpg"), width: 2000, height: 3000);

        // Act
        await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400);

        // Assert
        var outputFile = Directory.GetFiles(destDir, "*.webp").Single();
        using var resultImage = await Image.LoadAsync(outputFile);
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
        await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400);

        // Assert
        var outputFile = Directory.GetFiles(destDir, "*.webp").Single();
        using var resultImage = await Image.LoadAsync(outputFile);
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
        await _service.ProcessImagesAsync(sourceDir, destDir, targetWidth: 1400);

        // Assert
        var outputFile = Directory.GetFiles(destDir, "*.webp").Single();
        using var resultImage = await Image.LoadAsync(outputFile);
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
        var result = await _service.ProcessImagesAsync(sourceDir, destDir);

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
            Path.Combine(_tempDir, "dest"));

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
        await _service.ProcessImagesAsync(sourceDir, destDir);

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
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "ComicInfo.xml"), "<ComicInfo/>");

        // Act
        await _service.ProcessImagesAsync(sourceDir, destDir);

        // Assert
        File.Exists(Path.Combine(destDir, "ComicInfo.xml")).Should().BeTrue();
    }
}

using System.IO.Compression;
using Domain.Errors;
using Persistence.Services;

namespace Application.UnitTests.Services;

public sealed class ComicArchiveBuilderServiceTests : IDisposable
{
    private readonly ComicArchiveBuilderService _service = new();
    private readonly string _tempDir;

    public ComicArchiveBuilderServiceTests()
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

    private string CreateSourceDir(int webpCount = 3, bool includeComicInfo = false)
    {
        var sourceDir = Path.Combine(_tempDir, "source");
        Directory.CreateDirectory(sourceDir);

        for (var i = 1; i <= webpCount; i++)
        {
            File.WriteAllBytes(Path.Combine(sourceDir, $"page-{i:D3}.webp"), [0x52, 0x49, 0x46, 0x46]);
        }

        if (includeComicInfo)
        {
            File.WriteAllText(Path.Combine(sourceDir, "ComicInfo.xml"), "<ComicInfo><Title>Test</Title></ComicInfo>");
        }

        return sourceDir;
    }

    [Fact]
    public async Task BuildAsync_Should_CreateValidCbzFile()
    {
        // Arrange
        var sourceDir = CreateSourceDir(2);
        var outputPath = Path.Combine(_tempDir, "output.cbz");

        // Act
        var result = await _service.BuildAsync(sourceDir, outputPath, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public async Task BuildAsync_Should_IncludeAllWebpImages()
    {
        // Arrange
        var sourceDir = CreateSourceDir(3);
        var outputPath = Path.Combine(_tempDir, "output.cbz");

        // Act
        await _service.BuildAsync(sourceDir, outputPath, TestContext.Current.CancellationToken);

        // Assert
#pragma warning disable CA1849 // ZipFile has no OpenReadAsync overload
        using var zip = ZipFile.OpenRead(outputPath);
#pragma warning restore CA1849
        zip.Entries.Where(e => e.Name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)).Should().HaveCount(3);
    }

    [Fact]
    public async Task BuildAsync_Should_IncludeComicInfoXml_WhenPresent()
    {
        // Arrange
        var sourceDir = CreateSourceDir(2, includeComicInfo: true);
        var outputPath = Path.Combine(_tempDir, "output.cbz");

        // Act
        await _service.BuildAsync(sourceDir, outputPath, TestContext.Current.CancellationToken);

        // Assert
#pragma warning disable CA1849 // ZipFile has no OpenReadAsync overload
        using var zip = ZipFile.OpenRead(outputPath);
#pragma warning restore CA1849
        zip.Entries.Should().Contain(e => e.Name == "ComicInfo.xml");
    }

    [Fact]
    public async Task BuildAsync_Should_ExcludeComicInfoXml_WhenAbsent()
    {
        // Arrange
        var sourceDir = CreateSourceDir(2, includeComicInfo: false);
        var outputPath = Path.Combine(_tempDir, "output.cbz");

        // Act
        await _service.BuildAsync(sourceDir, outputPath, TestContext.Current.CancellationToken);

        // Assert
#pragma warning disable CA1849 // ZipFile has no OpenReadAsync overload
        using var zip = ZipFile.OpenRead(outputPath);
#pragma warning restore CA1849
        zip.Entries.Should().NotContain(e => e.Name == "ComicInfo.xml");
    }

    [Fact]
    public async Task BuildAsync_Should_ReturnCorrectPageCount()
    {
        // Arrange
        var sourceDir = CreateSourceDir(5);
        var outputPath = Path.Combine(_tempDir, "output.cbz");

        // Act
        var result = await _service.BuildAsync(sourceDir, outputPath, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PageCount.Should().Be(5);
    }

    [Fact]
    public async Task BuildAsync_Should_ReturnCorrectFileSize()
    {
        // Arrange
        var sourceDir = CreateSourceDir(2);
        var outputPath = Path.Combine(_tempDir, "output.cbz");

        // Act
        var result = await _service.BuildAsync(sourceDir, outputPath, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.FileSize.Should().Be(new FileInfo(outputPath).Length);
        result.Value.FileSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task BuildAsync_Should_ReturnError_WhenSourceDirectoryIsEmpty()
    {
        // Arrange
        var emptyDir = Path.Combine(_tempDir, "empty");
        Directory.CreateDirectory(emptyDir);
        var outputPath = Path.Combine(_tempDir, "output.cbz");

        // Act
        var result = await _service.BuildAsync(emptyDir, outputPath, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileProcessingError.EmptyDirectory);
    }

    [Fact]
    public async Task BuildAsync_Should_ReturnError_WhenSourceDirectoryDoesNotExist()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDir, "output.cbz");

        // Act
        var result = await _service.BuildAsync(Path.Combine(_tempDir, "nonexistent"), outputPath, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileProcessingError.InvalidPath);
    }
}

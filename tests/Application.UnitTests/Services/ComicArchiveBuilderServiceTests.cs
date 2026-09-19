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

    private List<string> CreateWebpFiles(int webpCount = 3)
    {
        var webpFiles = new List<string>();
        for (var i = 1; i <= webpCount; i++)
        {
            var path = Path.Combine(_tempDir, $"page-{i:D3}.webp");
            File.WriteAllBytes(path, [0x52, 0x49, 0x46, 0x46]);
            webpFiles.Add(path);
        }
        return webpFiles;
    }

    private string CreateComicInfoXml()
    {
        var path = Path.Combine(_tempDir, "ComicInfo.xml");
        File.WriteAllText(path, "<ComicInfo><Title>Test</Title></ComicInfo>");
        return path;
    }

    [Fact]
    public async Task BuildAsync_Should_CreateValidCbzFile()
    {
        // Arrange
        var webpFiles = CreateWebpFiles(2);
        var outputPath = Path.Combine(_tempDir, "output.cbz");

        // Act
        var result = await _service.BuildAsync(webpFiles, null, outputPath, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public async Task BuildAsync_Should_IncludeAllWebpImages()
    {
        // Arrange
        var webpFiles = CreateWebpFiles(3);
        var outputPath = Path.Combine(_tempDir, "output.cbz");

        // Act
        await _service.BuildAsync(webpFiles, null, outputPath, TestContext.Current.CancellationToken);

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
        var webpFiles = CreateWebpFiles(2);
        var comicInfoXmlPath = CreateComicInfoXml();
        var outputPath = Path.Combine(_tempDir, "output.cbz");

        // Act
        await _service.BuildAsync(webpFiles, comicInfoXmlPath, outputPath, TestContext.Current.CancellationToken);

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
        var webpFiles = CreateWebpFiles(2);
        var outputPath = Path.Combine(_tempDir, "output.cbz");

        // Act
        await _service.BuildAsync(webpFiles, null, outputPath, TestContext.Current.CancellationToken);

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
        var webpFiles = CreateWebpFiles(5);
        var outputPath = Path.Combine(_tempDir, "output.cbz");

        // Act
        var result = await _service.BuildAsync(webpFiles, null, outputPath, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PageCount.Should().Be(5);
    }

    [Fact]
    public async Task BuildAsync_Should_ReturnCorrectFileSize()
    {
        // Arrange
        var webpFiles = CreateWebpFiles(2);
        var outputPath = Path.Combine(_tempDir, "output.cbz");

        // Act
        var result = await _service.BuildAsync(webpFiles, null, outputPath, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.FileSize.Should().Be(new FileInfo(outputPath).Length);
        result.Value.FileSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task BuildAsync_Should_ReturnError_WhenWebpFilesIsEmpty()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDir, "output.cbz");

        // Act
        var result = await _service.BuildAsync([], null, outputPath, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileProcessingError.EmptyDirectory);
    }
}

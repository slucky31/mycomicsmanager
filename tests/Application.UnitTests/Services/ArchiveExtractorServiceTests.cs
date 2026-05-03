using System.IO.Compression;
using Domain.Errors;
using Persistence.Services;

namespace Application.UnitTests.Services;

public sealed class ArchiveExtractorServiceTests : IDisposable
{
    private readonly ArchiveExtractorService _service = new();
    private readonly string _tempDir;

    public ArchiveExtractorServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        // Force GC to release any native file handles held by SharpCompress on Windows
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        GC.SuppressFinalize(this);
    }

    private string CreateZipArchive(IEnumerable<(string name, string content)> entries, string fileName = "test.cbz")
    {
        var archivePath = Path.Combine(_tempDir, fileName);
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            var entry = archive.CreateEntry(name);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream);
            writer.Write(content);
        }
        return archivePath;
    }

    private string CreateDestDir()
    {
        var dir = Path.Combine(_tempDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        return dir;
    }

    // -------------------------------------------------------
    // CanHandle
    // -------------------------------------------------------

    [Fact]
    public void CanHandle_Should_ReturnTrue_ForCbzExtension()
        => _service.CanHandle("comic.cbz").Should().BeTrue();

    [Fact]
    public void CanHandle_Should_ReturnTrue_ForZipExtension()
        => _service.CanHandle("archive.zip").Should().BeTrue();

    [Fact]
    public void CanHandle_Should_ReturnTrue_ForCbrExtension()
        => _service.CanHandle("comic.cbr").Should().BeTrue();

    [Fact]
    public void CanHandle_Should_ReturnTrue_ForRarExtension()
        => _service.CanHandle("archive.rar").Should().BeTrue();

    [Fact]
    public void CanHandle_Should_ReturnFalse_ForPdfExtension()
        => _service.CanHandle("document.pdf").Should().BeFalse();

    [Fact]
    public void CanHandle_Should_ReturnFalse_ForUnknownExtension()
        => _service.CanHandle("file.7z").Should().BeFalse();

    // -------------------------------------------------------
    // ExtractAsync
    // -------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_Should_ReturnSuccess_WhenValidZipArchive()
    {
        // Arrange
        var archivePath = CreateZipArchive([("page-001.jpg", "fake-image-data")]);
        var destDir = CreateDestDir();

        // Act
        var result = await _service.ExtractAsync(archivePath, destDir, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ExtractedFiles.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExtractAsync_Should_FilterUnsupportedFileExtensions()
    {
        // Arrange
        var archivePath = CreateZipArchive([
            ("page-001.jpg", "fake-image"),
            ("readme.txt", "text file"),
            ("metadata.json", "json data"),
            ("page-002.png", "fake-png"),
        ]);
        var destDir = CreateDestDir();

        // Act
        var result = await _service.ExtractAsync(archivePath, destDir, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ExtractedFiles.Should().HaveCount(2); // only jpg and png
    }

    [Fact]
    public async Task ExtractAsync_Should_DetectComicInfoXml()
    {
        // Arrange
        var archivePath = CreateZipArchive([
            ("page-001.jpg", "fake-image"),
            ("ComicInfo.xml", "<ComicInfo><Title>Test</Title></ComicInfo>"),
        ]);
        var destDir = CreateDestDir();

        // Act
        var result = await _service.ExtractAsync(archivePath, destDir, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ComicInfoXmlPath.Should().NotBeNull();
        result.Value.ComicInfoXmlPath.Should().EndWith("ComicInfo.xml");
    }

    [Fact]
    public async Task ExtractAsync_Should_ReturnSuccess_WhenNoComicInfoXml()
    {
        // Arrange
        var archivePath = CreateZipArchive([("page-001.jpg", "fake-image")]);
        var destDir = CreateDestDir();

        // Act
        var result = await _service.ExtractAsync(archivePath, destDir, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ComicInfoXmlPath.Should().BeNull();
    }

    [Fact]
    public async Task ExtractAsync_Should_ReturnError_WhenFileDoesNotExist()
    {
        // Act
        var result = await _service.ExtractAsync(
            Path.Combine(_tempDir, "nonexistent.cbz"),
            CreateDestDir(),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileProcessingError.FileNotFound);
    }

    [Fact]
    public async Task ExtractAsync_Should_ReturnError_WhenArchiveIsCorrupt()
    {
        // Arrange
        var corruptPath = Path.Combine(_tempDir, "corrupt.cbz");
        await File.WriteAllBytesAsync(corruptPath, [0x00, 0x01, 0x02, 0x03, 0x04], TestContext.Current.CancellationToken);
        var destDir = CreateDestDir();

        // Act
        var result = await _service.ExtractAsync(corruptPath, destDir, TestContext.Current.CancellationToken);
        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ExtractAsync_Should_ReturnSortedFiles()
    {
        // Arrange
        var archivePath = CreateZipArchive([
            ("page-003.jpg", "fake"),
            ("page-001.jpg", "fake"),
            ("page-002.png", "fake"),
        ]);
        var destDir = CreateDestDir();

        // Act
        var result = await _service.ExtractAsync(archivePath, destDir, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var names = result.Value!.ExtractedFiles.Select(Path.GetFileName).ToList();
        names.Should().BeInAscendingOrder();
    }
}

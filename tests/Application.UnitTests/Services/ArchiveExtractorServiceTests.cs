using System.IO.Compression;
using Domain.Errors;
using Persistence.Services;

namespace Application.UnitTests.Services;

public sealed class ArchiveExtractorServiceTests : IDisposable
{
    private readonly ArchiveExtractorService _service = new(NullLogger<ArchiveExtractorService>.Instance);
    private readonly string _tempDir;

    public ArchiveExtractorServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        // Force GC to release any native file handles held by SharpCompress on Windows
#pragma warning disable S1215 // The finalizers of SharpCompress streams are the only way to release their file handles
        GC.Collect();
#pragma warning restore S1215
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
    public async Task ExtractAsync_Should_ReturnCorruptArchive_WhenZipIsTruncatedBeforeCentralDirectory()
    {
        // Arrange
        var archivePath = CreateZipArchive([("page-001.jpg", "fake-image-data"), ("page-002.jpg", "fake-image-data")]);
        var bytes = await File.ReadAllBytesAsync(archivePath, TestContext.Current.CancellationToken);
        var centralDirectoryStart = bytes.AsSpan().IndexOf("PK\u0001\u0002"u8);
        await File.WriteAllBytesAsync(archivePath, bytes[..centralDirectoryStart], TestContext.Current.CancellationToken);
        var destDir = CreateDestDir();

        // Act
        var result = await _service.ExtractAsync(archivePath, destDir, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileProcessingError.CorruptArchive);
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

    // -------------------------------------------------------
    // ExtractAsync — archive of archives
    // -------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_Should_ExtractTheInnerArchives_WhenTheArchiveHasNoPage()
    {
        var archivePath = CreateZipArchive(
        [
            ("Serie - T02.cbz", "volume 2"),
            ("folder/Serie - T01.cbz", "volume 1"),
            ("Serie - T03.pdf", "volume 3"),
            ("._Serie - T04.cbz", "macOS resource fork"),
            ("readme.txt", "text")
        ], "volumes.zip");
        var destDir = CreateDestDir();

        var result = await _service.ExtractAsync(archivePath, destDir, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ExtractedFiles.Should().BeEmpty();
        result.Value.ComicInfoXmlPath.Should().BeNull();
        result.Value.NestedArchives.Select(Path.GetFileName)
            .Should().Equal("Serie - T01.cbz", "Serie - T02.cbz", "Serie - T03.pdf");
        result.Value.NestedArchives.Should().OnlyContain(f => File.Exists(f));
    }

    [Fact]
    public async Task ExtractAsync_Should_IgnoreTheInnerArchives_WhenTheArchiveHasPages()
    {
        var archivePath = CreateZipArchive([("page01.jpg", "img"), ("bonus.cbz", "volume")]);
        var destDir = CreateDestDir();

        var result = await _service.ExtractAsync(archivePath, destDir, TestContext.Current.CancellationToken);

        result.Value!.ExtractedFiles.Select(Path.GetFileName).Should().Equal("page01.jpg");
        result.Value.NestedArchives.Should().BeEmpty();
        File.Exists(Path.Combine(destDir, "bonus.cbz")).Should().BeFalse();
    }

    [Fact]
    public async Task ExtractAsync_Should_ReturnNoFile_WhenTheArchiveHasNeitherPageNorArchive()
    {
        var archivePath = CreateZipArchive([("ComicInfo.xml", "<ComicInfo/>"), ("readme.txt", "text")]);
        var destDir = CreateDestDir();

        var result = await _service.ExtractAsync(archivePath, destDir, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ExtractedFiles.Should().BeEmpty();
        result.Value.NestedArchives.Should().BeEmpty();
    }

    // -------------------------------------------------------
    // ExtractAsync — limits (#1370)
    // -------------------------------------------------------

    private static byte[] ZipBytes(params (string Name, byte[] Content)[] entries)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var stream = archive.CreateEntry(name).Open();
                stream.Write(content);
            }
        }
        return ms.ToArray();
    }

    private string WriteArchive(string fileName, byte[] bytes)
    {
        var path = Path.Combine(_tempDir, fileName);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    // Sets the uncompressed size of every entry, in its local header and in the central directory, to 1 byte.
    private static byte[] WithFalsifiedSizes(byte[] zip)
    {
        for (var i = 0; i + 30 < zip.Length; i++)
        {
            var signature = BitConverter.ToUInt32(zip, i);
            var sizeOffset = signature switch
            {
                0x04034b50 => i + 22,
                0x02014b50 => i + 24,
                _ => -1
            };
            if (sizeOffset > 0)
            {
                BitConverter.GetBytes(1u).CopyTo(zip, sizeOffset);
            }
        }
        return zip;
    }

    [Fact]
    public async Task ExtractAsync_Should_StopAtTheDeclaredSize_WhenAnEntryHoldsMore()
    {
        // The audit fixture: a small zip whose page declares 1 byte but expands to 1 MiB.
        var archivePath = WriteArchive("lying.cbz", WithFalsifiedSizes(ZipBytes(("page01.jpg", new byte[1024 * 1024]))));
        var destDir = CreateDestDir();

        var result = await _service.ExtractAsync(archivePath, destDir, TestContext.Current.CancellationToken);

        result.Error.Should().Be(FileProcessingError.ExtractionLimitExceeded);
        Directory.GetFiles(destDir).Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_Should_ReturnInsufficientDiskSpace_WhenTheDeclaredSizeDoesNotFit()
    {
        var service = new ArchiveExtractorService(
            NullLogger<ArchiveExtractorService>.Instance, ExtractionLimits.Default with { AvailableBytes = _ => 10 });
        var archivePath = WriteArchive("big.cbz", ZipBytes(("page01.jpg", new byte[100])));

        var result = await service.ExtractAsync(archivePath, CreateDestDir(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(Domain.ImportJobs.ImportJobError.InsufficientDiskSpace);
    }

    [Fact]
    public async Task ExtractAsync_Should_ExtractTheVolumes_WhenTheyAreArchivesOfPages()
    {
        // #1295: an archive holding several volumes is still split.
        var volume = ZipBytes(("page01.jpg", [1, 2, 3]));
        var archivePath = WriteArchive("volumes.zip", ZipBytes(("Serie - T01.cbz", volume), ("Serie - T02.cbz", volume)));

        var result = await _service.ExtractAsync(archivePath, CreateDestDir(), TestContext.Current.CancellationToken);

        result.Value!.NestedArchives.Select(Path.GetFileName).Should().Equal("Serie - T01.cbz", "Serie - T02.cbz");
    }

    [Fact]
    public async Task ExtractAsync_Should_RefuseTheArchive_WhenAVolumeHoldsArchivesAgain()
    {
        // The audit fixture: 3 levels of 3 archives each.
        var volume = ZipBytes(("page01.jpg", [1, 2, 3]));
        var middle = ZipBytes(("a.cbz", volume), ("b.cbz", volume), ("c.cbz", volume));
        var archivePath = WriteArchive("nested.zip", ZipBytes(("1.zip", middle), ("2.zip", middle), ("3.zip", middle)));

        var result = await _service.ExtractAsync(archivePath, CreateDestDir(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(FileProcessingError.NestedArchiveTooDeep);
    }

    [Fact]
    public void AvailableBytes_Should_MeasureTheClosestExistingFolder_WhenTheFolderIsNotCreatedYet()
    {
        var notCreated = Path.Combine(_tempDir, "not", "created");

        DiskSpace.AvailableBytes(notCreated).Should().BeInRange(1, long.MaxValue - 1);
    }
}

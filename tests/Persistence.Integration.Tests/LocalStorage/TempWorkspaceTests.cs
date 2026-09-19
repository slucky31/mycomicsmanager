using Application.ImportJobs;
using Application.Libraries;
using Domain.ImportJobs;
using Domain.Primitives;
using Microsoft.Extensions.Options;
using Persistence.LocalStorage;

namespace Persistence.Tests.LocalStorage;

public sealed class TempWorkspaceTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("temp-workspace-tests-").FullName;
    private readonly TempWorkspace _sut;

    public TempWorkspaceTests()
    {
        var settings = Options.Create(new ImportSettings { TempDirectory = _root });
        _sut = new TempWorkspace(settings, new FakeLibraryLocalStorage(_root));
    }

    [Fact]
    public void MoveToLibrary_Should_ReturnDestinationFileExists_WhenFileAlreadyPresent()
    {
        // Arrange
        var libraryDir = Path.Combine(_root, "lib");
        Directory.CreateDirectory(libraryDir);
        File.WriteAllText(Path.Combine(libraryDir, "book.cbz"), "existing");

        var sourcePath = Path.Combine(_root, "source.cbz");
        File.WriteAllText(sourcePath, "new");

        // Act
        var result = _sut.MoveToLibrary(sourcePath, "lib", "book.cbz");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.DestinationFileExists);
        File.Exists(sourcePath).Should().BeTrue();
        File.ReadAllText(Path.Combine(libraryDir, "book.cbz")).Should().Be("existing");
    }

    [Fact]
    public void MoveToLibrary_Should_MoveFile_WhenDestinationIsFree()
    {
        // Arrange
        var sourcePath = Path.Combine(_root, "source.cbz");
        File.WriteAllText(sourcePath, "content");

        // Act
        var result = _sut.MoveToLibrary(sourcePath, "lib", "book.cbz");

        // Assert
        result.IsSuccess.Should().BeTrue();
        File.Exists(sourcePath).Should().BeFalse();
        File.Exists(Path.Combine(_root, "lib", "book.cbz")).Should().BeTrue();
    }

    [Fact]
    public void HasFreeSpace_Should_ReturnTrue_WhenRequiredBytesIsSmall()
    {
        var result = _sut.HasFreeSpace(1);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasFreeSpace_Should_ReturnFalse_WhenRequiredBytesExceedsAvailableSpace()
    {
        var result = _sut.HasFreeSpace(long.MaxValue);

        result.Should().BeFalse();
    }

    [Fact]
    public void CreateScratch_Should_ReturnRawAndConvertedSubdirectories_UnderJobIdFolder()
    {
        var jobId = Guid.CreateVersion7();

        var (tempDir, rawDir, convertedDir) = _sut.CreateScratch(jobId);

        tempDir.Should().Be(Path.Combine(_root, jobId.ToString()));
        rawDir.Should().Be(Path.Combine(tempDir, "raw"));
        convertedDir.Should().Be(Path.Combine(tempDir, "converted"));
    }

    [Fact]
    public void GetWebpFiles_Should_ReturnEmpty_WhenDirectoryDoesNotExist()
    {
        var result = _sut.GetWebpFiles(Path.Combine(_root, "missing"));

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetWebpFiles_Should_ReturnOnlyWebpFiles_SortedByName()
    {
        var dir = Path.Combine(_root, "converted");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "page-002.webp"), "b");
        File.WriteAllText(Path.Combine(dir, "page-001.webp"), "a");
        File.WriteAllText(Path.Combine(dir, "ComicInfo.xml"), "meta");

        var result = _sut.GetWebpFiles(dir);

        result.Should().HaveCount(2);
        result[0].Should().EndWith("page-001.webp");
        result[1].Should().EndWith("page-002.webp");
    }

    [Fact]
    public void TryDeleteFile_Should_RemoveFile_WhenItExists()
    {
        var filePath = Path.Combine(_root, "temp.cbz");
        File.WriteAllText(filePath, "content");

        _sut.TryDeleteFile(filePath);

        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public void TryDeleteFile_Should_NotThrow_WhenFileDoesNotExist()
    {
        var act = () => _sut.TryDeleteFile(Path.Combine(_root, "missing.cbz"));

        act.Should().NotThrow();
    }

    [Fact]
    public void CleanupDirectory_Should_RemoveDirectoryRecursively_WhenItExists()
    {
        var dir = Path.Combine(_root, "scratch");
        Directory.CreateDirectory(Path.Combine(dir, "sub"));
        File.WriteAllText(Path.Combine(dir, "sub", "file.txt"), "content");

        _sut.CleanupDirectory(dir);

        Directory.Exists(dir).Should().BeFalse();
    }

    [Fact]
    public void CleanupDirectory_Should_DoNothing_WhenDirectoryDoesNotExist()
    {
        var act = () => _sut.CleanupDirectory(Path.Combine(_root, "missing"));

        act.Should().NotThrow();
    }

    public void Dispose()
    {
        Directory.Delete(_root, true);
    }

    private sealed class FakeLibraryLocalStorage(string rootPath) : ILibraryLocalStorage
    {
        public string rootPath { get; init; } = rootPath;
        public Result Create(string folderName) => Result.Success();
        public Result Delete(string folderName) => Result.Success();
        public Result Move(string originFolderName, string destinationFolderName) => Result.Success();
    }
}

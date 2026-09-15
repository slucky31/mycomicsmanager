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

using Application.ImportJobs;
using Domain.Errors;
using Microsoft.Extensions.Options;
using Persistence.LocalStorage;

namespace Persistence.Tests.LocalStorage;

public sealed class ImportDirectoryStorageTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("import-dir-storage-tests-").FullName;
    private readonly ImportDirectoryStorage _sut;

    public ImportDirectoryStorageTests()
    {
        var settings = Options.Create(new ImportSettings { ImportDirectory = _root });
        _sut = new ImportDirectoryStorage(settings);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    // ── EnsureExists ──────────────────────────────────────────────────────────

    [Fact]
    public void EnsureExists_Should_CreateDirectory_WhenPathIsValid()
    {
        var result = _sut.EnsureExists("MYCOMICS_lib1");

        result.IsSuccess.Should().BeTrue();
        Directory.Exists(Path.Combine(_root, "MYCOMICS_lib1")).Should().BeTrue();
    }

    [Fact]
    public void EnsureExists_Should_ReturnArgumentNullOrEmpty_WhenDirectoryNameIsEmpty()
    {
        var result = _sut.EnsureExists(string.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportDirectoryStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void EnsureExists_Should_ReturnInvalidPath_WhenDirectoryNameEscapesRoot()
    {
        var result = _sut.EnsureExists("../outside");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportDirectoryStorageError.InvalidPath);
    }

    // ── Move ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Move_Should_MoveDirectory_WhenOriginExistsAndDestinationIsFree()
    {
        Directory.CreateDirectory(Path.Combine(_root, "origin"));

        var result = _sut.Move("origin", "destination");

        result.IsSuccess.Should().BeTrue();
        Directory.Exists(Path.Combine(_root, "origin")).Should().BeFalse();
        Directory.Exists(Path.Combine(_root, "destination")).Should().BeTrue();
    }

    [Fact]
    public void Move_Should_ReturnSuccess_WhenOriginDoesNotExist()
    {
        var result = _sut.Move("missing-origin", "destination");

        result.IsSuccess.Should().BeTrue();
        Directory.Exists(Path.Combine(_root, "destination")).Should().BeFalse();
    }

    [Fact]
    public void Move_Should_ReturnDestinationAlreadyExists_WhenDestinationDirectoryPresent()
    {
        Directory.CreateDirectory(Path.Combine(_root, "origin"));
        Directory.CreateDirectory(Path.Combine(_root, "destination"));

        var result = _sut.Move("origin", "destination");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportDirectoryStorageError.DestinationAlreadyExists);
    }

    [Fact]
    public void Move_Should_ReturnArgumentNullOrEmpty_WhenEitherNameIsEmpty()
    {
        var result = _sut.Move(string.Empty, "destination");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportDirectoryStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void Move_Should_ReturnInvalidPath_WhenDestinationEscapesRoot()
    {
        Directory.CreateDirectory(Path.Combine(_root, "origin"));

        var result = _sut.Move("origin", "../outside");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportDirectoryStorageError.InvalidPath);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_Should_RemoveDirectory_WhenItExists()
    {
        var dir = Path.Combine(_root, "to-delete");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "file.txt"), "content");

        var result = _sut.Delete("to-delete");

        result.IsSuccess.Should().BeTrue();
        Directory.Exists(dir).Should().BeFalse();
    }

    [Fact]
    public void Delete_Should_ReturnSuccess_WhenDirectoryDoesNotExist()
    {
        var result = _sut.Delete("never-existed");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Delete_Should_ReturnArgumentNullOrEmpty_WhenDirectoryNameIsEmpty()
    {
        var result = _sut.Delete(string.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportDirectoryStorageError.ArgumentNullOrEmpty);
    }

    // ── DeleteOriginalFile ────────────────────────────────────────────────────

    [Fact]
    public void DeleteOriginalFile_Should_RemoveFile_WhenItExists()
    {
        var filePath = Path.Combine(_root, "comic.cbz");
        File.WriteAllText(filePath, "content");

        var result = _sut.DeleteOriginalFile(filePath);

        result.IsSuccess.Should().BeTrue();
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public void DeleteOriginalFile_Should_ReturnSuccess_WhenFileDoesNotExist()
    {
        var result = _sut.DeleteOriginalFile(Path.Combine(_root, "missing.cbz"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void DeleteOriginalFile_Should_ReturnArgumentNullOrEmpty_WhenPathIsEmpty()
    {
        var result = _sut.DeleteOriginalFile(string.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportDirectoryStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void DeleteOriginalFile_Should_ReturnInvalidPath_WhenPathEscapesRoot()
    {
        var outsideFile = Path.Combine(Path.GetTempPath(), "outside.cbz");

        var result = _sut.DeleteOriginalFile(outsideFile);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportDirectoryStorageError.InvalidPath);
    }

    // ── MoveOriginalFileToError ───────────────────────────────────────────────

    [Fact]
    public void MoveOriginalFileToError_Should_MoveFileToErrorsSubdirectory()
    {
        var libraryDir = Path.Combine(_root, "MYCOMICS_lib1");
        Directory.CreateDirectory(libraryDir);
        var filePath = Path.Combine(libraryDir, "comic.cbz");
        File.WriteAllText(filePath, "content");

        var result = _sut.MoveOriginalFileToError(filePath);

        result.IsSuccess.Should().BeTrue();
        File.Exists(filePath).Should().BeFalse();
        File.Exists(Path.Combine(libraryDir, "errors", "comic.cbz")).Should().BeTrue();
    }

    [Fact]
    public void MoveOriginalFileToError_Should_AppendTimestamp_WhenDestinationAlreadyExists()
    {
        var libraryDir = Path.Combine(_root, "MYCOMICS_lib1");
        var errorsDir = Path.Combine(libraryDir, "errors");
        Directory.CreateDirectory(errorsDir);
        File.WriteAllText(Path.Combine(errorsDir, "comic.cbz"), "already-there");

        var filePath = Path.Combine(libraryDir, "comic.cbz");
        File.WriteAllText(filePath, "new-content");

        var result = _sut.MoveOriginalFileToError(filePath);

        result.IsSuccess.Should().BeTrue();
        File.Exists(filePath).Should().BeFalse();
        var errorFiles = Directory.GetFiles(errorsDir);
        errorFiles.Should().HaveCount(2);
        errorFiles.Should().Contain(f => f != Path.Combine(errorsDir, "comic.cbz") && Path.GetFileName(f).StartsWith("comic_", StringComparison.Ordinal));
    }

    [Fact]
    public void MoveOriginalFileToError_Should_ReturnSuccess_WhenFileDoesNotExist()
    {
        var result = _sut.MoveOriginalFileToError(Path.Combine(_root, "missing.cbz"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void MoveOriginalFileToError_Should_ReturnArgumentNullOrEmpty_WhenPathIsEmpty()
    {
        var result = _sut.MoveOriginalFileToError(string.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportDirectoryStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void MoveOriginalFileToError_Should_ReturnInvalidPath_WhenPathEscapesRoot()
    {
        var outsideFile = Path.Combine(Path.GetTempPath(), "outside.cbz");

        var result = _sut.MoveOriginalFileToError(outsideFile);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportDirectoryStorageError.InvalidPath);
    }
}

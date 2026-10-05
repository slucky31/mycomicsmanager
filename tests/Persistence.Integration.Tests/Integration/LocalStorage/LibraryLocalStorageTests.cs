using System.Text;
using Base.Integration.Tests;
using Domain.Books;
using Domain.Errors;
using Persistence.LocalStorage;

namespace Persistence.Tests.Integration.LocalStorage;

[Collection("DatabaseCollectionTests")]
public class LibraryLocalStorageTests(IntegrationTestWebAppFactory factory) : LibraryLocalStorageIntegrationTest(factory)
{
    private readonly LibraryLocalStorage _libraryLocalStorageWithEmptyRootPath = new("");

    private static void CreateFile(string path)
    {
        using var fs = File.Create(path);
        var info = new UTF8Encoding(true).GetBytes("This is some text in the file.");
        fs.Write(info, 0, info.Length);
    }

    [Fact]
    public void Create_ShouldCreateDirectory()
    {
        // Arrange
        var folder = Guid.NewGuid().ToString();

        // Act
        var result = LibraryLocalStorage.Create(folder);

        //Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(Path.Combine(LibraryLocalStorage.rootPath, folder)).Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldReturnError_WhenRootPathIsEmpty()
    {
        // Arrange
        var folder = Guid.NewGuid().ToString();

        // Act
        var result = _libraryLocalStorageWithEmptyRootPath.Create(folder);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void Create_ShouldReturnError_WhenFolderNameIsEmpty()
    {
        // Arrange        
        var folder = string.Empty;

        // Act
        var result = LibraryLocalStorage.Create(folder);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void Move_ShouldMoveDirectory()
    {
        // Arrange        
        var folder = Guid.NewGuid().ToString();
        var result = LibraryLocalStorage.Create(folder);
        result.IsSuccess.Should().BeTrue();
        var folderMoved = Guid.NewGuid().ToString();

        // Act
        result = LibraryLocalStorage.Move(folder, folderMoved);

        //Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(Path.Combine(LibraryLocalStorage.rootPath, folder)).Should().BeFalse();
        Directory.Exists(Path.Combine(LibraryLocalStorage.rootPath, folderMoved)).Should().BeTrue();
    }

    [Fact]
    public void Move_ShouldMoveDirectoryAndFiles()
    {
        // Arrange
        var folder = Guid.NewGuid().ToString();
        var result = LibraryLocalStorage.Create(folder);
        result.IsSuccess.Should().BeTrue();
        var fileName = Guid.NewGuid().ToString() + ".txt";
        var filePath = LibraryLocalStorage.rootPath + Path.DirectorySeparatorChar + folder + Path.DirectorySeparatorChar + fileName;
        CreateFile(filePath);
        File.Exists(filePath).Should().BeTrue();
        var folderMoved = Guid.NewGuid().ToString();

        // Act
        result = LibraryLocalStorage.Move(folder, folderMoved);

        //Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(LibraryLocalStorage.rootPath + Path.DirectorySeparatorChar + folder).Should().BeFalse();
        Directory.Exists(LibraryLocalStorage.rootPath + Path.DirectorySeparatorChar + folderMoved).Should().BeTrue();
        File.Exists(LibraryLocalStorage.rootPath + Path.DirectorySeparatorChar + folder + Path.DirectorySeparatorChar + fileName).Should().BeFalse();
        File.Exists(LibraryLocalStorage.rootPath + Path.DirectorySeparatorChar + folderMoved + Path.DirectorySeparatorChar + fileName).Should().BeTrue();
    }

    [Fact]
    public void Move_ShouldReturnError_WhenDestinationFolderAlreadyExists()
    {
        // Arrange        
        var folder = Guid.NewGuid().ToString();
        var result = LibraryLocalStorage.Create(folder);
        result.IsSuccess.Should().BeTrue();
        var folderMoved = Guid.NewGuid().ToString();
        result = LibraryLocalStorage.Create(folderMoved);
        result.IsSuccess.Should().BeTrue();

        // Act
        result = LibraryLocalStorage.Move(folder, folderMoved);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.AlreadyExistingFolder);
    }

    [Fact]
    public void Move_ShouldReturnError_WhenRootPathIsEmpty()
    {
        // Arrange
        var folder = Guid.NewGuid().ToString();
        var folderMoved = Guid.NewGuid().ToString();

        // Act
        var result = _libraryLocalStorageWithEmptyRootPath.Move(folder, folderMoved);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void Move_ShouldReturnError_WhenOldFolderNameIsEmpty()
    {
        // Arrange        
        var folder = string.Empty;
        var folderMoved = Guid.NewGuid().ToString();

        // Act
        var result = LibraryLocalStorage.Move(folder, folderMoved);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void Move_ShouldReturnError_WhenNewFolderNameIsEmpty()
    {
        // Arrange        
        var folder = Guid.NewGuid().ToString();
        var result = LibraryLocalStorage.Create(folder);
        result.IsSuccess.Should().BeTrue();
        const string folderMoved = "";

        // Act
        result = LibraryLocalStorage.Move(folder, folderMoved);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void Move_ShouldReturnError_WhenOriginFolderNameNotExists()
    {
        // Arrange        
        var folder = Guid.NewGuid().ToString();
        Directory.Exists(LibraryLocalStorage.rootPath + Path.DirectorySeparatorChar + folder).Should().BeFalse();
        var folderMoved = Guid.NewGuid().ToString();

        // Act
        var result = LibraryLocalStorage.Move(folder, folderMoved);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.UnknownFolder);
    }

    [Fact]
    public void Delete_ShouldDeleteDirectory()
    {
        // Arrange
        var folder = Guid.NewGuid().ToString();
        var result = LibraryLocalStorage.Create(folder);
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(LibraryLocalStorage.rootPath + Path.DirectorySeparatorChar + folder).Should().BeTrue();

        // Act
        result = LibraryLocalStorage.Delete(folder);

        //Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(LibraryLocalStorage.rootPath + Path.DirectorySeparatorChar + folder).Should().BeFalse();
    }

    [Fact]
    public void Delete_ShouldDeleteDirectoryAndFiles()
    {
        // Arrange
        var folder = Guid.NewGuid().ToString();
        var result = LibraryLocalStorage.Create(folder);
        result.IsSuccess.Should().BeTrue();
        var fileName = Guid.NewGuid().ToString() + ".txt";
        var path = LibraryLocalStorage.rootPath + Path.DirectorySeparatorChar + folder;
        var filePath = path + Path.DirectorySeparatorChar + fileName;
        CreateFile(filePath);
        File.Exists(filePath).Should().BeTrue();

        // Act
        result = LibraryLocalStorage.Delete(folder);

        //Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(path).Should().BeFalse();
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public void Delete_ShouldReturnError_WhenFolderNameIsEmpty()
    {
        // Arrange
        const string folder = "";

        // Act
        var result = LibraryLocalStorage.Delete(folder);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void Delete_ShouldReturnError_WhenRootPathIsEmpty()
    {
        // Arrange
        var folder = Guid.NewGuid().ToString();

        // Act
        var result = _libraryLocalStorageWithEmptyRootPath.Delete(folder);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void Delete_ShouldReturnError_WhenFolderNotExists()
    {
        // Arrange
        var folder = Guid.NewGuid().ToString();
        Directory.Exists(LibraryLocalStorage.rootPath + Path.DirectorySeparatorChar + folder).Should().BeFalse();

        // Act
        var result = LibraryLocalStorage.Delete(folder);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.UnknownFolder);
    }

    [Fact]
    public void Create_ShouldReturnError_WhenFolderNameContainsInvalidPathChars()
    {
        // Arrange – null character causes Path.GetFullPath to throw ArgumentException
        var folder = "invalid\0name";

        // Act
        var result = LibraryLocalStorage.Create(folder);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.InvalidPath);
    }

    [Fact]
    public void Move_ShouldReturnError_WhenOriginFolderNameContainsInvalidPathChars()
    {
        // Arrange
        var folder = "invalid\0name";

        // Act
        var result = LibraryLocalStorage.Move(folder, "destination");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.InvalidPath);
    }

    [Fact]
    public void Move_ShouldReturnError_WhenDestinationFolderNameContainsInvalidPathChars()
    {
        // Arrange
        var folder = Guid.NewGuid().ToString();
        LibraryLocalStorage.Create(folder);

        // Act
        var result = LibraryLocalStorage.Move(folder, "invalid\0name");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.InvalidPath);
    }

    [Fact]
    public void Delete_ShouldReturnError_WhenFolderNameContainsInvalidPathChars()
    {
        // Arrange – null character causes Path.GetFullPath to throw ArgumentException
        var folder = "invalid\0name";

        // Act
        var result = LibraryLocalStorage.Delete(folder);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.InvalidPath);
    }


    [Fact]
    public void Create_ShouldReturnError_WhenFolderNameTraversesOutsideRoot()
    {
        // Act
        var result = LibraryLocalStorage.Create("../../etc");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.InvalidPath);
    }

    [Fact]
    public void Move_ShouldReturnError_WhenOriginFolderNameTraversesOutsideRoot()
    {
        // Act
        var result = LibraryLocalStorage.Move("../../etc", "destination");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.InvalidPath);
    }

    [Fact]
    public void Move_ShouldReturnError_WhenDestinationFolderNameTraversesOutsideRoot()
    {
        // Arrange
        var folder = Guid.NewGuid().ToString();
        LibraryLocalStorage.Create(folder);

        // Act
        var result = LibraryLocalStorage.Move(folder, "../../etc");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.InvalidPath);
    }

    [Fact]
    public void Delete_ShouldReturnError_WhenFolderNameTraversesOutsideRoot()
    {
        // Act
        var result = LibraryLocalStorage.Delete("../../etc");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.InvalidPath);
    }

    [Fact]
    public void MoveFile_Should_MoveFileIntoTargetLibraryFolder_AndReturnNewPath()
    {
        // Arrange
        var source = Guid.NewGuid().ToString();
        var target = $"Bandes dessinées {Guid.NewGuid()}";
        Directory.CreateDirectory(Path.Combine(LibraryLocalStorage.rootPath, source));
        var sourceFile = Path.Combine(LibraryLocalStorage.rootPath, source, "Blacksad T03.cbz");
        CreateFile(sourceFile);

        // Act
        var result = LibraryLocalStorage.MoveFile(sourceFile, target);

        // Assert
        var expected = Path.Combine(LibraryLocalStorage.rootPath, target.Replace("é", "e", StringComparison.Ordinal), "Blacksad T03.cbz");
        result.Value.Should().Be(expected);
        File.Exists(expected).Should().BeTrue();
        File.Exists(sourceFile).Should().BeFalse();
    }

    [Fact]
    public void MoveFile_Should_KeepBothFiles_WhenTargetAlreadyHasAFileWithSameName()
    {
        // Arrange
        var source = Guid.NewGuid().ToString();
        var target = Guid.NewGuid().ToString();
        Directory.CreateDirectory(Path.Combine(LibraryLocalStorage.rootPath, source));
        Directory.CreateDirectory(Path.Combine(LibraryLocalStorage.rootPath, target));
        var sourceFile = Path.Combine(LibraryLocalStorage.rootPath, source, "Blacksad T03.cbz");
        CreateFile(sourceFile);
        CreateFile(Path.Combine(LibraryLocalStorage.rootPath, target, "Blacksad T03.cbz"));

        // Act
        var result = LibraryLocalStorage.MoveFile(sourceFile, target);

        // Assert
        result.Error.Should().Be(BooksError.FileAlreadyExists);
        File.Exists(sourceFile).Should().BeTrue();
    }

    [Fact]
    public void MoveFile_Should_ReturnError_WhenSourceIsMissingOrOutsideRoot()
    {
        LibraryLocalStorage.MoveFile(Path.Combine(LibraryLocalStorage.rootPath, "missing.cbz"), "BD").Error.Should().Be(BooksError.FileNotFound);
        LibraryLocalStorage.MoveFile(Path.Combine(Path.GetTempPath(), "outside.cbz"), "BD").Error.Should().Be(LibraryLocalStorageError.InvalidPath);
        LibraryLocalStorage.MoveFile(Path.Combine(LibraryLocalStorage.rootPath, "x.cbz"), "../outside").Error.Should().Be(LibraryLocalStorageError.InvalidPath);
    }
}

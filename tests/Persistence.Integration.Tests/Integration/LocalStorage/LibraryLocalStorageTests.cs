using System.Text;
using Base.Integration.Tests;
using Domain.Errors;
using Persistence.LocalStorage;

namespace Persistence.Tests.Integration.LocalStorage;

[Collection("LocalStorage")]
public class LibraryLocalStorageTests(IntegrationTestWebAppFactory factory) : LibraryLocalStorageIntegrationTest(factory)
{
    private readonly LibraryLocalStorage LibraryLocalStorageWithEmptyRootPath = new("");

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
        Directory.Exists(LibraryLocalStorage.rootPath + Path.DirectorySeparatorChar + folder).Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldReturnError_WhenRootPathIsEmpty()
    {
        // Arrange
        var folder = Guid.NewGuid().ToString();

        // Act
        var result = LibraryLocalStorageWithEmptyRootPath.Create(folder);

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
        Directory.Exists(LibraryLocalStorage.rootPath + Path.DirectorySeparatorChar + folder).Should().BeFalse();
        Directory.Exists(LibraryLocalStorage.rootPath + Path.DirectorySeparatorChar + folderMoved).Should().BeTrue();
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
        var result = LibraryLocalStorageWithEmptyRootPath.Move(folder, folderMoved);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void Move_ShouldReturnError_WhenOldFolderNameIsEmpty()
    {
        // Arrange        
        const string folder = "";
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
        var result = LibraryLocalStorage.Delete(folder);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.UnknownFolder);
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

}

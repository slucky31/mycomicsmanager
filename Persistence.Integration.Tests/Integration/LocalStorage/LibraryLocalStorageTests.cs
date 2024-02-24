using System.Text;
using Domain.Errors;
using Persistence.LocalStorage;

namespace Persistence.Tests.Integration.LocalStorage;

public class LibraryLocalStorageTests
{
    private static void CreateFile(string path)
    {
        using FileStream fs = File.Create(path);
        byte[] info = new UTF8Encoding(true).GetBytes("This is some text in the file.");
        fs.Write(info, 0, info.Length);
    }
    
    [Fact]
    public void Create_ShouldCreateDirectory()
    {
        // Arrange
        var rootpath = Path.GetTempPath();
        var folder = Guid.NewGuid().ToString();

        // Act
        var result = LibraryLocalStorage.Create(rootpath, folder);

        //Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(rootpath + Path.DirectorySeparatorChar + folder).Should().BeTrue();        
    }

    [Fact]
    public void Create_ShouldReturnError_WhenRootPathIsEmpty()
    {
        // Arrange
        string rootpath = string.Empty;
        var folder = Guid.NewGuid().ToString();

        // Act
        var result= LibraryLocalStorage.Create(rootpath, folder);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void Create_ShouldReturnError_WhenFolderNameIsEmpty()
    {
        // Arrange
        string rootpath = Guid.NewGuid().ToString();
        var folder = string.Empty;

        // Act
        var result = LibraryLocalStorage.Create(rootpath, folder);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void Move_ShouldMoveDirectory()
    {
        // Arrange
        var rootpath = Path.GetTempPath();
        var folder = Guid.NewGuid().ToString();
        var result = LibraryLocalStorage.Create(rootpath, folder);
        result.IsSuccess.Should().BeTrue();
        var folderMoved = Guid.NewGuid().ToString();

        // Act
        result = LibraryLocalStorage.Move(rootpath, folder, folderMoved);

        //Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(rootpath + Path.DirectorySeparatorChar + folder).Should().BeFalse();
        Directory.Exists(rootpath + Path.DirectorySeparatorChar + folderMoved).Should().BeTrue();
    }

    [Fact]
    public void Move_ShouldMoveDirectoryAndFiles()
    {
        // Arrange
        var rootpath = Path.GetTempPath();
        var folder = Guid.NewGuid().ToString();
        var result = LibraryLocalStorage.Create(rootpath, folder);
        result.IsSuccess.Should().BeTrue();
        var fileName = Guid.NewGuid().ToString() + ".txt";
        var filePath = rootpath + Path.DirectorySeparatorChar + folder + Path.DirectorySeparatorChar + fileName;
        CreateFile(filePath);
        File.Exists(filePath).Should().BeTrue();
        var folderMoved = Guid.NewGuid().ToString();

        // Act
        result = LibraryLocalStorage.Move(rootpath, folder, folderMoved);

        //Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(rootpath + Path.DirectorySeparatorChar + folder).Should().BeFalse();
        Directory.Exists(rootpath + Path.DirectorySeparatorChar + folderMoved).Should().BeTrue();
        File.Exists(rootpath + Path.DirectorySeparatorChar + folder + Path.DirectorySeparatorChar + fileName).Should().BeFalse();
        File.Exists(rootpath + Path.DirectorySeparatorChar + folderMoved + Path.DirectorySeparatorChar + fileName).Should().BeTrue();
    }

    [Fact]
    public void Move_ShouldReturnError_WhenDestinationFolderAlreadyExists()
    {
        // Arrange
        var rootpath = Path.GetTempPath();
        var folder = Guid.NewGuid().ToString();
        var result = LibraryLocalStorage.Create(rootpath, folder);
        result.IsSuccess.Should().BeTrue();
        var folderMoved = Guid.NewGuid().ToString();
        result = LibraryLocalStorage.Create(rootpath, folderMoved);
        result.IsSuccess.Should().BeTrue();

        // Act
        result = LibraryLocalStorage.Move(rootpath, folder, folderMoved);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.AlreadyExistingFolder);
    }

    [Fact]
    public void Move_ShouldReturnError_WhenRootPathIsEmpty()
    {
        // Arrange
        var rootpath = "";
        var folder = Guid.NewGuid().ToString();        
        var folderMoved = Guid.NewGuid().ToString();

        // Act
        var result = LibraryLocalStorage.Move(rootpath, folder, folderMoved);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void Move_ShouldReturnError_WhenOldFolderNameIsEmpty()
    {
        // Arrange
        var rootpath = Path.GetTempPath();
        var folder = "";        
        var folderMoved = Guid.NewGuid().ToString();

        // Act
        var result = LibraryLocalStorage.Move(rootpath, folder, folderMoved);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void Move_ShouldReturnError_WhenNewFolderNameIsEmpty()
    {
        // Arrange
        var rootpath = Path.GetTempPath();
        var folder = Guid.NewGuid().ToString();
        var result = LibraryLocalStorage.Create(rootpath, folder);
        result.IsSuccess.Should().BeTrue();
        var folderMoved = "";

        // Act
        result = LibraryLocalStorage.Move(rootpath, folder, folderMoved);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void Move_ShouldReturnError_WhenOriginFolderNameNotExists()
    {
        // Arrange
        var rootpath = Path.GetTempPath();
        var folder = Guid.NewGuid().ToString();
        Directory.Exists(rootpath + Path.DirectorySeparatorChar + folder).Should().BeFalse();
        var folderMoved = Guid.NewGuid().ToString();

        // Act
        var result = LibraryLocalStorage.Move(rootpath, folder, folderMoved);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.UnknownFolder);
    }

    [Fact]
    public void Delete_ShouldDeleteDirectory()
    {
        // Arrange
        var rootpath = Path.GetTempPath();
        var folder = Guid.NewGuid().ToString();
        var result = LibraryLocalStorage.Create(rootpath, folder);
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(rootpath + Path.DirectorySeparatorChar + folder).Should().BeTrue();

        // Act
        result = LibraryLocalStorage.Delete(rootpath, folder);

        //Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(rootpath + Path.DirectorySeparatorChar + folder).Should().BeFalse();        
    }

    [Fact]
    public void Delete_ShouldDeleteDirectoryAndFiles()
    {
        // Arrange
        var rootpath = Path.GetTempPath();
        var folder = Guid.NewGuid().ToString();
        var result = LibraryLocalStorage.Create(rootpath, folder);
        result.IsSuccess.Should().BeTrue();
        var fileName = Guid.NewGuid().ToString() + ".txt";
        var path = rootpath + Path.DirectorySeparatorChar + folder;
        var filePath = path + Path.DirectorySeparatorChar + fileName;
        CreateFile(filePath);
        File.Exists(filePath).Should().BeTrue();

        // Act
        result = LibraryLocalStorage.Delete(rootpath, folder);

        //Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(path).Should().BeFalse();        
        File.Exists(filePath).Should().BeFalse();        
    }

    [Fact]
    public void Delete_ShouldReturnError_WhenFolderNameIsEmpty()
    {
        // Arrange
        var rootpath = Path.GetTempPath();
        var folder = "";      

        // Act
        var result = LibraryLocalStorage.Delete(rootpath, folder);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void Delete_ShouldReturnError_WhenRootPathIsEmpty()
    {
        // Arrange
        var rootpath = "";
        var folder = Guid.NewGuid().ToString();

        // Act
        var result = LibraryLocalStorage.Delete(rootpath, folder);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.ArgumentNullOrEmpty);
    }

    [Fact]
    public void Delete_ShouldReturnError_WhenFolderNotExists()
    {
        // Arrange
        var rootpath = Path.GetTempPath();
        var folder = Guid.NewGuid().ToString();
        Directory.Exists(rootpath + Path.DirectorySeparatorChar + folder).Should().BeFalse();        

        // Act
        var result = LibraryLocalStorage.Delete(rootpath, folder);

        //Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibraryLocalStorageError.UnknownFolder);
    }

}

using Application.ImportJobs;
using Application.Interfaces;
using Application.Libraries;
using Application.Libraries.Delete;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Libraries;

public class DeleteLibraryCommandTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly DeleteLibraryCommand s_command = new(Guid.CreateVersion7(), s_userId);
    private static readonly Library s_library = Library.Create("test", "#5C6BC0", "Bookmark", LibraryBookType.Physical, s_userId).Value!;
    private static readonly Library s_digitalLibrary = Library.Create("digital-test", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;

    private readonly DeleteLibraryCommandHandler _handler;
    private readonly IRepository<Library, Guid> _librayRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ILibraryLocalStorage _libraryLocalStorageMock;
    private readonly IImportDirectoryStorage _importDirectoryStorageMock;
    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();

    public DeleteLibraryCommandTests()
    {
        _librayRepositoryMock = Substitute.For<IRepository<Library, Guid>>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _unitOfWorkMock.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success<int>(1));
        _libraryLocalStorageMock = Substitute.For<ILibraryLocalStorage>();
        _importDirectoryStorageMock = Substitute.For<IImportDirectoryStorage>();
        _importDirectoryStorageMock.Delete(Arg.Any<string>()).Returns(Result.Success());
        _bookRepository.ListByLibraryIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        _libraryLocalStorageMock.ListFiles(Arg.Any<string>()).Returns([]);

        _handler = new DeleteLibraryCommandHandler(_librayRepositoryMock, _unitOfWorkMock, _libraryLocalStorageMock, _importDirectoryStorageMock, _bookRepository);
    }

    [Fact]
    public async Task Handle_Should_ReturnValidationError_WhenRequestIsNull()
    {
        // Act
        var result = await _handler.Handle(null!, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.ValidationError);
        _librayRepositoryMock.DidNotReceive().Remove(Arg.Any<Library>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenLibraryIsNull()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns((Library?)null);

        // Act
        var result = await _handler.Handle(s_command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
        _librayRepositoryMock.Received(0).Remove(Arg.Any<Library>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenUserDoesNotOwnLibrary()
    {
        // Arrange
        var anotherUserId = Guid.CreateVersion7();
        var commandFromOtherUser = new DeleteLibraryCommand(s_command.Id, anotherUserId);
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_library);

        // Act
        var result = await _handler.Handle(commandFromOtherUser, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_ForPhysicalLibrary()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_library);

        // Act
        var result = await _handler.Handle(s_command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _librayRepositoryMock.Received(1).Remove(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _libraryLocalStorageMock.DidNotReceive().Delete(Arg.Any<string>());
        _importDirectoryStorageMock.DidNotReceive().Delete(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_ForDigitalLibrary()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_digitalLibrary);
        _libraryLocalStorageMock.Delete(s_digitalLibrary.RelativePath).Returns(Result.Success());

        // Act
        var result = await _handler.Handle(s_command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _librayRepositoryMock.Received(1).Remove(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _importDirectoryStorageMock.Received(1).Delete(s_digitalLibrary.ImportDirectoryName);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenDirectoryIsNotDeleted()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_digitalLibrary);
        _libraryLocalStorageMock.Delete(s_digitalLibrary.RelativePath).Returns(Result.Failure(new TError("Test")));

        // Act
        var result = await _handler.Handle(s_command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.FolderNotDeleted);
    }

    [Fact]
    public async Task Handle_Should_RefuseToDeleteTheImportedLibrary()
    {
        // Arrange
        var imported = Library.Create("Imported", "#5C6BC0", "Inbox", LibraryBookType.Digital, s_userId).Value!;
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(imported);

        // Act
        var result = await _handler.Handle(s_command, TestContext.Current.CancellationToken);

        // Assert
        result.Error.Should().Be(LibrariesError.ImportedLibraryProtected);
        _libraryLocalStorageMock.DidNotReceive().Delete(Arg.Any<string>());
        _librayRepositoryMock.DidNotReceive().Remove(Arg.Any<Library>());
    }

    [Fact]
    public async Task Handle_Should_KeepTheFilesOfAnotherLibrary_WhenBothNamesGiveTheSameFolder()
    {
        // Arrange: "Comics" and "Cômics" share the COMICS folder (#1369), with a real storage.
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var storage = new Persistence.LocalStorage.LibraryLocalStorage(root);
            var comics = Library.Create("Comics", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;
            var accented = Library.Create("Cômics", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;
            storage.Create(comics.RelativePath);
            var file = Path.Combine(root, "COMICS", "Blacksad T01.cbz");
            await File.WriteAllTextAsync(file, "comic", TestContext.Current.CancellationToken);
            _librayRepositoryMock.GetByIdAsync(accented.Id).Returns(accented);
            var handler = new DeleteLibraryCommandHandler(_librayRepositoryMock, _unitOfWorkMock, storage, _importDirectoryStorageMock, _bookRepository);

            // Act
            var result = await handler.Handle(new DeleteLibraryCommand(accented.Id, s_userId), TestContext.Current.CancellationToken);

            // Assert
            result.Error.Should().Be(LibrariesError.FolderHasForeignFiles);
            File.Exists(file).Should().BeTrue();
            _librayRepositoryMock.DidNotReceive().Remove(Arg.Any<Library>());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Handle_Should_DeleteTheFolder_WhenItHoldsOnlyTheBooksOfTheLibrary()
    {
        // Arrange
        var library = Library.Create("digital-only", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;
        var book = Domain.Books.DigitalBook.Create(new Domain.Books.BookMetadata("Serie", "Title", null), library.Id, "/data/DIGITAL-ONLY/a.cbz", 10).Value!;
        _librayRepositoryMock.GetByIdAsync(library.Id).Returns(library);
        _bookRepository.ListByLibraryIdAsync(library.Id, Arg.Any<CancellationToken>()).Returns([book]);
        _libraryLocalStorageMock.ListFiles("DIGITAL-ONLY").Returns(["/data/DIGITAL-ONLY/a.cbz"]);
        _libraryLocalStorageMock.Delete("DIGITAL-ONLY").Returns(Result.Success());

        // Act
        var result = await _handler.Handle(new DeleteLibraryCommand(library.Id, s_userId), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _libraryLocalStorageMock.Received(1).Delete("DIGITAL-ONLY");
    }
}

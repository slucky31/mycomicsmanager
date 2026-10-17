using Application.ImportJobs;
using Application.Interfaces;
using Application.Libraries;
using Application.Libraries.Update;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Libraries;

public class UpdateLibraryCommandTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly UpdateLibraryCommand s_command = new(Guid.CreateVersion7(), "library", "#5C6BC0", "Bookmark", s_userId);
    private static readonly Library s_library = Library.Create("library", "#5C6BC0", "Bookmark", LibraryBookType.Physical, s_userId).Value!;

    private readonly UpdateLibraryCommandHandler _handler;
    private readonly IRepository<Library, Guid> _librayRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ILibraryReadService _libraryReadServiceMock;
    private readonly ILibraryLocalStorage _libraryLocalStorage;
    private readonly IImportDirectoryStorage _importDirectoryStorageMock;
    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();

    public UpdateLibraryCommandTests()
    {
        _librayRepositoryMock = Substitute.For<IRepository<Library, Guid>>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _unitOfWorkMock.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _libraryReadServiceMock = Substitute.For<ILibraryReadService>();
        _libraryLocalStorage = Substitute.For<ILibraryLocalStorage>();
        _importDirectoryStorageMock = Substitute.For<IImportDirectoryStorage>();
        _importDirectoryStorageMock.Move(Arg.Any<string>(), Arg.Any<string>()).Returns(Result.Success());
        _bookRepository.ListByLibraryIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);

        _handler = new UpdateLibraryCommandHandler(_librayRepositoryMock, _unitOfWorkMock, _libraryReadServiceMock, _libraryLocalStorage, _importDirectoryStorageMock, _bookRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenRequestIsNull()
    {
        // Act
        var result = await _handler.Handle(null!, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.ValidationError);
        _librayRepositoryMock.DidNotReceive().Update(Arg.Any<Library>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenColorIsInvalid()
    {
        // Arrange – empty color makes library.Update() fail
        var commandWithBadColor = new UpdateLibraryCommand(s_command.Id, null, "", "Bookmark", s_userId);
        _librayRepositoryMock.GetByIdAsync(commandWithBadColor.Id).Returns(s_library);

        // Act
        var result = await _handler.Handle(commandWithBadColor, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BadRequest);
        _librayRepositoryMock.DidNotReceive().Update(Arg.Any<Library>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenNameIsInvalidEmptyString()
    {
        // Arrange – empty string name is not null (enters name-update path) but fails UpdateName()
        var commandWithEmptyName = new UpdateLibraryCommand(s_command.Id, "", "#5C6BC0", "Bookmark", s_userId);
        _librayRepositoryMock.GetByIdAsync(commandWithEmptyName.Id).Returns(s_library);

        // Act
        var result = await _handler.Handle(commandWithEmptyName, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BadRequest);
        _librayRepositoryMock.DidNotReceive().Update(Arg.Any<Library>());
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_ForDigitalLibraryWithNameChange()
    {
        // Arrange
        var digitalCommand = new UpdateLibraryCommand(s_command.Id, "new-digital-name", "#5C6BC0", "Bookmark", s_userId);
        var freshDigitalLibrary = Library.Create("digital-library", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;
        _librayRepositoryMock.GetByIdAsync(digitalCommand.Id).Returns(freshDigitalLibrary);
        _libraryLocalStorage.Move(Arg.Any<string>(), Arg.Any<string>()).Returns(Result.Success());

        // Act
        var result = await _handler.Handle(digitalCommand, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("new-digital-name");
        _libraryLocalStorage.Received(1).Move(Arg.Any<string>(), Arg.Any<string>());
        _importDirectoryStorageMock.Received(1).Move(Arg.Any<string>(), Arg.Any<string>());
        _librayRepositoryMock.Received(1).Update(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenLibraryIsNotFound()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns((Library?)null);

        // Act
        var result = await _handler.Handle(s_command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
        _librayRepositoryMock.Received(0).Update(Arg.Any<Library>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserDoesNotOwnLibrary()
    {
        // Arrange
        var anotherUserId = Guid.CreateVersion7();
        var commandFromOtherUser = new UpdateLibraryCommand(s_command.Id, "new-name", "#5C6BC0", "Bookmark", anotherUserId);
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_library);

        // Act
        var result = await _handler.Handle(commandFromOtherUser, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnDuplicate_WhenALibraryWithSameNameAlreadyExist()
    {
        // Arrange
        var renameCommand = s_command with { Name = "Comics" };
        _libraryReadServiceMock.ExistsByNameAsync("Comics", s_command.UserId, s_command.Id, Arg.Any<CancellationToken>()).Returns(true);
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_library);

        // Act
        var result = await _handler.Handle(renameCommand, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.Duplicate);
    }

    [Fact]
    public async Task Handle_ShouldReturnFolderNotMoved_WhenDirectoryWasNotMoved()
    {
        // Arrange
        var digitalCommand = new UpdateLibraryCommand(s_command.Id, "new-digital-name", "#5C6BC0", "Bookmark", s_userId);
        var freshDigitalLibrary = Library.Create("digital-library", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;
        _librayRepositoryMock.GetByIdAsync(digitalCommand.Id).Returns(freshDigitalLibrary);
        _libraryLocalStorage.Move(Arg.Any<string>(), Arg.Any<string>()).Returns(Result.Failure(new TError("Test")));

        // Act
        var result = await _handler.Handle(digitalCommand, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.FolderNotMoved);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_ForPhysicalLibraryWithNameChange()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_library);

        // Act
        var result = await _handler.Handle(s_command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.Name.Should().Be(s_command.Name);
        _librayRepositoryMock.Received(1).Update(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _libraryLocalStorage.DidNotReceive().Move(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WithoutNameChange()
    {
        // Arrange
        var commandWithoutName = new UpdateLibraryCommand(s_command.Id, null, "#FF0000", "Star", s_userId);
        _librayRepositoryMock.GetByIdAsync(commandWithoutName.Id).Returns(s_library);

        // Act
        var result = await _handler.Handle(commandWithoutName, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.Color.Should().Be("#FF0000");
        result.Value.Icon.Should().Be("Star");
        _librayRepositoryMock.Received(1).Update(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenDefaultBookSortOrderIsInvalid()
    {
        // Arrange
        var commandWithInvalidSortOrder = new UpdateLibraryCommand(s_command.Id, null, "#5C6BC0", "Bookmark", s_userId, (BookSortOrder)999);
        var freshLibrary = Library.Create("library", "#5C6BC0", "Bookmark", LibraryBookType.Physical, s_userId).Value!;
        _librayRepositoryMock.GetByIdAsync(commandWithInvalidSortOrder.Id).Returns(freshLibrary);

        // Act
        var result = await _handler.Handle(commandWithInvalidSortOrder, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BadRequest);
        _librayRepositoryMock.DidNotReceive().Update(Arg.Any<Library>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPersistDefaultBookSortOrder_WhenProvided()
    {
        // Arrange
        var commandWithSortOrder = new UpdateLibraryCommand(s_command.Id, null, "#5C6BC0", "Bookmark", s_userId, BookSortOrder.SerieAndVolumeAsc);
        var freshLibrary = Library.Create("library", "#5C6BC0", "Bookmark", LibraryBookType.Physical, s_userId).Value!;
        _librayRepositoryMock.GetByIdAsync(commandWithSortOrder.Id).Returns(freshLibrary);

        // Act
        var result = await _handler.Handle(commandWithSortOrder, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.DefaultBookSortOrder.Should().Be(BookSortOrder.SerieAndVolumeAsc);
        _librayRepositoryMock.Received(1).Update(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFolderNotMoved_WhenImportDirectoryWasNotMoved()
    {
        // Arrange
        var digitalCommand = new UpdateLibraryCommand(s_command.Id, "new-digital-name", "#5C6BC0", "Bookmark", s_userId);
        var freshDigitalLibrary = Library.Create("digital-library", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;
        _librayRepositoryMock.GetByIdAsync(digitalCommand.Id).Returns(freshDigitalLibrary);
        _libraryLocalStorage.Move(Arg.Any<string>(), Arg.Any<string>()).Returns(Result.Success());
        _importDirectoryStorageMock.Move(Arg.Any<string>(), Arg.Any<string>()).Returns(Result.Failure(new TError("Test")));

        // Act
        var result = await _handler.Handle(digitalCommand, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.FolderNotMoved);
    }

    private static DigitalBook DigitalBook(Library library, string filePath) =>
        Domain.Books.DigitalBook.Create(new BookMetadata("Serie", "Title", null), library.Id, filePath, 10).Value!;

    [Theory]
    [InlineData("digital-library")]
    [InlineData("Digital-Library")]
    public async Task Handle_Should_KeepTheFoldersOfADigitalLibrary_WhenItsFolderNameDoesNotChange(string name)
    {
        // Arrange
        var library = Library.Create("digital-library", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(library);

        // Act
        var result = await _handler.Handle(s_command with { Name = name, Color = "#FF0000" }, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be(name);
        result.Value.Color.Should().Be("#FF0000");
        _libraryLocalStorage.DidNotReceive().Move(Arg.Any<string>(), Arg.Any<string>());
        await _bookRepository.DidNotReceive().ListByLibraryIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_RefuseToRenameTheImportedLibrary_ButKeepItEditable()
    {
        // Arrange
        var imported = Library.Create("Imported", "#5C6BC0", "Inbox", LibraryBookType.Digital, s_userId).Value!;
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(imported);

        // Act
        var renamed = await _handler.Handle(s_command with { Name = "Comics" }, TestContext.Current.CancellationToken);
        var recolored = await _handler.Handle(s_command with { Name = "Imported", Color = "#FF0000" }, TestContext.Current.CancellationToken);

        // Assert
        renamed.Error.Should().Be(LibrariesError.ImportedLibraryProtected);
        recolored.IsSuccess.Should().BeTrue();
        recolored.Value!.Color.Should().Be("#FF0000");
        _libraryLocalStorage.DidNotReceive().Move(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Should_KeepThePathsOfTheBookFiles_WhenADigitalLibraryIsRenamed()
    {
        // Arrange
        var library = Library.Create("À trier", "#5C6BC0", "Inbox", LibraryBookType.Digital, s_userId).Value!;
        var inFolder = DigitalBook(library, "/data/A TRIER/blacksad.cbz");
        var elsewhere = DigitalBook(library, "/elsewhere/hiver.cbz");
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(library);
        _bookRepository.ListByLibraryIdAsync(library.Id, Arg.Any<CancellationToken>()).Returns([inFolder, elsewhere]);
        _libraryLocalStorage.PathAfterMove("/data/A TRIER/blacksad.cbz", "A TRIER", "IMPORTED").Returns("/data/IMPORTED/blacksad.cbz");
        _libraryLocalStorage.PathAfterMove("/elsewhere/hiver.cbz", "A TRIER", "IMPORTED").Returns((string?)null);
        _libraryLocalStorage.Move(Arg.Any<string>(), Arg.Any<string>()).Returns(Result.Success());

        // Act
        var result = await _handler.Handle(s_command with { Name = "Imported" }, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _libraryLocalStorage.Received(1).Move("A TRIER", "IMPORTED");
        _importDirectoryStorageMock.Received(1).Move($"A TRIER_{library.Id}", $"IMPORTED_{library.Id}");
        inFolder.FilePath.Should().Be("/data/IMPORTED/blacksad.cbz");
        elsewhere.FilePath.Should().Be("/elsewhere/hiver.cbz");
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_KeepTheFolders_WhenTheNewPathOfABookFileIsInvalid()
    {
        // Arrange
        var library = Library.Create("digital-library", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(library);
        _bookRepository.ListByLibraryIdAsync(library.Id, Arg.Any<CancellationToken>()).Returns([DigitalBook(library, "/data/DIGITAL-LIBRARY/a.cbz")]);
        _libraryLocalStorage.PathAfterMove(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(" ");

        // Act
        var result = await _handler.Handle(s_command with { Name = "Comics" }, TestContext.Current.CancellationToken);

        // Assert
        result.Error.Should().Be(BooksError.BadRequest);
        _libraryLocalStorage.DidNotReceive().Move(Arg.Any<string>(), Arg.Any<string>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

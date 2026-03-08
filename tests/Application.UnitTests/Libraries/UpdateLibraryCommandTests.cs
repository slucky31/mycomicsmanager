using Application.Interfaces;
using Application.Libraries;
using Application.Libraries.Update;
using Ardalis.GuardClauses;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Libraries;

public class UpdateLibraryCommandTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly UpdateLibraryCommand s_command = new(Guid.CreateVersion7(), "library", "#5C6BC0", "Bookmark", s_userId);
    private static readonly Library s_library = Library.Create("library", "#5C6BC0", "Bookmark", LibraryBookType.Physical, s_userId).Value!;
    private static readonly Library s_digitalLibrary = Library.Create("digital-library", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;

    private readonly UpdateLibraryCommandHandler _handler;
    private readonly IRepository<Library, Guid> _librayRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ILibraryReadService _libraryReadServiceMock;
    private readonly ILibraryLocalStorage _libraryLocalStorage;

    public UpdateLibraryCommandTests()
    {
        _librayRepositoryMock = Substitute.For<IRepository<Library, Guid>>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _libraryReadServiceMock = Substitute.For<ILibraryReadService>();
        _libraryLocalStorage = Substitute.For<ILibraryLocalStorage>();

        _handler = new UpdateLibraryCommandHandler(_librayRepositoryMock, _unitOfWorkMock, _libraryReadServiceMock, _libraryLocalStorage);
    }

    [Fact]
    public async Task Handle_Should_ReturnValidationError_WhenRequestIsNull()
    {
        // Act
        var result = await _handler.Handle(null!, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.ValidationError);
        _librayRepositoryMock.DidNotReceive().Update(Arg.Any<Library>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenColorIsInvalid()
    {
        // Arrange – empty color makes library.Update() fail
        var commandWithBadColor = new UpdateLibraryCommand(s_command.Id, null, "", "Bookmark", s_userId);
        _librayRepositoryMock.GetByIdAsync(commandWithBadColor.Id).Returns(s_library);

        // Act
        var result = await _handler.Handle(commandWithBadColor, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BadRequest);
        _librayRepositoryMock.DidNotReceive().Update(Arg.Any<Library>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenNameIsInvalidEmptyString()
    {
        // Arrange – empty string name is not null (enters name-update path) but fails UpdateName()
        var commandWithEmptyName = new UpdateLibraryCommand(s_command.Id, "", "#5C6BC0", "Bookmark", s_userId);
        _librayRepositoryMock.GetByIdAsync(commandWithEmptyName.Id).Returns(s_library);

        // Act
        var result = await _handler.Handle(commandWithEmptyName, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BadRequest);
        _librayRepositoryMock.DidNotReceive().Update(Arg.Any<Library>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_ForDigitalLibraryWithNameChange()
    {
        // Arrange
        var digitalCommand = new UpdateLibraryCommand(s_command.Id, "new-digital-name", "#5C6BC0", "Bookmark", s_userId);
        var freshDigitalLibrary = Library.Create("digital-library", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;
        _librayRepositoryMock.GetByIdAsync(digitalCommand.Id).Returns(freshDigitalLibrary);
        _libraryLocalStorage.Move(Arg.Any<string>(), Arg.Any<string>()).Returns(Result.Success());

        // Act
        var result = await _handler.Handle(digitalCommand, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("new-digital-name");
        _libraryLocalStorage.Received(1).Move(Arg.Any<string>(), Arg.Any<string>());
        _librayRepositoryMock.Received(1).Update(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenLibraryIsNotFound()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns((Library?)null);

        // Act
        var result = await _handler.Handle(s_command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
        _librayRepositoryMock.Received(0).Update(Arg.Any<Library>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenUserDoesNotOwnLibrary()
    {
        // Arrange
        var anotherUserId = Guid.CreateVersion7();
        var commandFromOtherUser = new UpdateLibraryCommand(s_command.Id, "new-name", "#5C6BC0", "Bookmark", anotherUserId);
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_library);

        // Act
        var result = await _handler.Handle(commandFromOtherUser, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnDuplicate_WhenALibraryWithSameNameAlreadyExist()
    {
        // Arrange
        _libraryReadServiceMock.ExistsByNameAsync(s_command.Name!, s_command.UserId, s_command.Id, Arg.Any<CancellationToken>()).Returns(true);
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_library);

        // Act
        var result = await _handler.Handle(s_command, default);

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
        _libraryLocalStorage.Move(Arg.Any<string>(), Arg.Any<string>()).Returns(Result.Failure(TError.Any));

        // Act
        var result = await _handler.Handle(digitalCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.FolderNotMoved);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_ForPhysicalLibraryWithNameChange()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_library);

        // Act
        var result = await _handler.Handle(s_command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.Name.Should().Be(s_command.Name);
        _librayRepositoryMock.Received(1).Update(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
        _libraryLocalStorage.DidNotReceive().Move(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WithoutNameChange()
    {
        // Arrange
        var commandWithoutName = new UpdateLibraryCommand(s_command.Id, null, "#FF0000", "Star", s_userId);
        _librayRepositoryMock.GetByIdAsync(commandWithoutName.Id).Returns(s_library);

        // Act
        var result = await _handler.Handle(commandWithoutName, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.Color.Should().Be("#FF0000");
        result.Value.Icon.Should().Be("Star");
        _librayRepositoryMock.Received(1).Update(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
    }
}

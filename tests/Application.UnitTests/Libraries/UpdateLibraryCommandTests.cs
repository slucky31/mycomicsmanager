using Application.Interfaces;
using Application.Libraries;
using Application.Libraries.Update;
using Ardalis.GuardClauses;
using Domain.Libraries;
using Domain.Primitives;
using MockQueryable;
using NSubstitute;
using Persistence.Queries.Helpers;

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
        List<Library> list = [s_library];
        var query = list.BuildMock();
        var pagedList = new PagedList<Library>(query);
        await pagedList.ExecuteQueryAsync(1, 2);
        _libraryReadServiceMock.GetLibrariesAsync(s_command.Name, LibrariesColumn.Name, null, 1, 1, s_command.UserId, Arg.Any<CancellationToken>()).Returns(pagedList);
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
        _librayRepositoryMock.GetByIdAsync(digitalCommand.Id).Returns(s_digitalLibrary);
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

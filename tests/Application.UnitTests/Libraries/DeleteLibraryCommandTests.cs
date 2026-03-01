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

    public DeleteLibraryCommandTests()
    {
        _librayRepositoryMock = Substitute.For<IRepository<Library, Guid>>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _libraryLocalStorageMock = Substitute.For<ILibraryLocalStorage>();

        _handler = new DeleteLibraryCommandHandler(_librayRepositoryMock, _unitOfWorkMock, _libraryLocalStorageMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenLibraryIsNull()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns((Library?)null);

        // Act
        var result = await _handler.Handle(s_command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
        _librayRepositoryMock.Received(0).Remove(Arg.Any<Library>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenUserDoesNotOwnLibrary()
    {
        // Arrange
        var anotherUserId = Guid.CreateVersion7();
        var commandFromOtherUser = new DeleteLibraryCommand(s_command.Id, anotherUserId);
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_library);

        // Act
        var result = await _handler.Handle(commandFromOtherUser, default);

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
        var result = await _handler.Handle(s_command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _librayRepositoryMock.Received(1).Remove(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
        _libraryLocalStorageMock.DidNotReceive().Delete(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_ForDigitalLibrary()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_digitalLibrary);
        _libraryLocalStorageMock.Delete(s_digitalLibrary.RelativePath).Returns(Result.Success());

        // Act
        var result = await _handler.Handle(s_command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _librayRepositoryMock.Received(1).Remove(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenDirectoryIsNotDeleted()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_digitalLibrary);
        _libraryLocalStorageMock.Delete(s_digitalLibrary.RelativePath).Returns(Result.Failure(TError.Any));

        // Act
        var result = await _handler.Handle(s_command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.FolderNotDeleted);
    }
}

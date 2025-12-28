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
    private static readonly UpdateLibraryCommand s_command = new(Guid.CreateVersion7(), "library");
    private static readonly Library s_library = Library.Create("library");

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
    public async Task Handle_Should_ReturnError_WhenLibraryIsNotFoundAsync()
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
    public async Task Handle_ShouldReturnDuplicate_WhenALibraryWithSameNameAlreadyExistAsync()
    {
        // Arrange
        List<Library> list = [s_library];
        var query = list.BuildMock();
        var pagedList = new PagedList<Library>(query);
        await pagedList.ExecuteQueryAsync(1, 2);
        _libraryReadServiceMock.GetLibrariesAsync(s_command.Name, LibrariesColumn.Name, null, 1, 1).Returns(pagedList);
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_library);

        // Act
        var result = await _handler.Handle(s_command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.Duplicate);
    }

    [Fact]
    public async Task Handle_ShouldReturnFolderNotMoved_WhenDirectoryWasNotMovedAsync()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_library);
        _libraryLocalStorage.Move(Arg.Any<string>(), Arg.Any<string>()).Returns(Result.Failure(TError.Any));

        // Act
        var result = await _handler.Handle(s_command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.FolderNotMoved);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessAsync()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_library);
        _libraryLocalStorage.Move(Arg.Any<string>(), Arg.Any<string>()).Returns(Result.Success());

        // Act
        var result = await _handler.Handle(s_command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.Name.Should().Be(s_command.Name);
        _librayRepositoryMock.Received(1).Update(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
    }

}

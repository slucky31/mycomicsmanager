using Application.Interfaces;
using Application.Libraries;
using Application.Libraries.CreateDefault;
using Ardalis.GuardClauses;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Libraries;

public class CreateDefaultLibraryCommandTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly CreateDefaultLibraryCommand s_command = new(s_userId);

    private readonly CreateDefaultLibraryCommandHandler _handler;
    private readonly IRepository<Library, Guid> _libraryRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ILibraryReadService _libraryReadServiceMock;

    public CreateDefaultLibraryCommandTests()
    {
        _libraryRepositoryMock = Substitute.For<IRepository<Library, Guid>>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _libraryReadServiceMock = Substitute.For<ILibraryReadService>();

        _handler = new CreateDefaultLibraryCommandHandler(_libraryRepositoryMock, _unitOfWorkMock, _libraryReadServiceMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenNoDefaultLibraryExists()
    {
        // Arrange
        _libraryReadServiceMock.GetDefaultLibraryAsync(s_userId, Arg.Any<CancellationToken>()).Returns((Library?)null);

        // Act
        var result = await _handler.Handle(s_command, default);
        Guard.Against.Null(result.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDefault.Should().BeTrue();
        result.Value.UserId.Should().Be(s_userId);
        result.Value.Name.Should().Be(LibraryConstants.DefaultLibraryName);
        _libraryRepositoryMock.Received(1).Add(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Should_ReturnExistingLibrary_WhenDefaultAlreadyExists()
    {
        // Arrange
        var existingDefault = Library.CreateDefault(s_userId);
        _libraryReadServiceMock.GetDefaultLibraryAsync(s_userId, Arg.Any<CancellationToken>()).Returns(existingDefault);

        // Act
        var result = await _handler.Handle(s_command, default);
        Guard.Against.Null(result.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existingDefault);
        _libraryRepositoryMock.DidNotReceive().Add(Arg.Any<Library>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ExecuteSaveChangeAsyncOnce_WhenNewLibraryCreated()
    {
        // Arrange
        _libraryReadServiceMock.GetDefaultLibraryAsync(s_userId, Arg.Any<CancellationToken>()).Returns((Library?)null);

        // Act
        await _handler.Handle(s_command, default);

        // Assert
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreateLibraryWithAllType()
    {
        // Arrange
        _libraryReadServiceMock.GetDefaultLibraryAsync(s_userId, Arg.Any<CancellationToken>()).Returns((Library?)null);

        // Act
        var result = await _handler.Handle(s_command, default);
        Guard.Against.Null(result.Value);

        // Assert
        result.Value.BookType.Should().Be(LibraryBookType.All);
    }
}

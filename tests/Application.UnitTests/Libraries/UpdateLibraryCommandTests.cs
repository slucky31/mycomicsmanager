using Application.Data;
using Domain.Libraries;
using NSubstitute;
using Application.Interfaces;
using MongoDB.Bson;
using Application.Libraries.Update;
using Ardalis.GuardClauses;
using MockQueryable.NSubstitute;
using Persistence.Queries.Helpers;
using Application.Libraries;

namespace Application.UnitTests.Libraries;
public class UpdateLibraryCommandTests
{
    private static readonly UpdateLibraryCommand Command = new(new ObjectId(), "library");
    private static readonly Library library = Library.Create("library");

    private readonly UpdateLibraryCommandHandler _handler;
    private readonly IRepository<Library, ObjectId> _librayRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ILibraryReadService _libraryReadServiceMock;

    public UpdateLibraryCommandTests()
    {
        _librayRepositoryMock = Substitute.For<IRepository<Library, ObjectId>>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _libraryReadServiceMock = Substitute.For<ILibraryReadService>();

        _handler = new UpdateLibraryCommandHandler(_librayRepositoryMock, _unitOfWorkMock, _libraryReadServiceMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenLibraryIsNotFound()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(Command.Id).Returns((Library?)null);

        // Act
        var result = await _handler.Handle(Command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
        _librayRepositoryMock.Received(0).Update(Arg.Any<Library>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ShouldReturnDuplicate_WhenALibraryWithSameNameAlreadyExist()
    {
        // Arrange
        List<Library> list = [library];
        var query = list.AsQueryable().BuildMock();
        var pagedList = new PagedList<Library>(query);
        await pagedList.ExecuteQueryAsync(1, 2);
        _libraryReadServiceMock.GetLibrariesAsync(Command.Name, LibrariesColumn.Name, null, 1, 1).Returns(pagedList);
        _librayRepositoryMock.GetByIdAsync(Command.Id).Returns(library);

        // Act
        var result = await _handler.Handle(Command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.Duplicate);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(Command.Id).Returns(library);

        // Act
        var result = await _handler.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.Name.Should().Be(Command.Name);
        _librayRepositoryMock.Received(1).Update(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
    }

}

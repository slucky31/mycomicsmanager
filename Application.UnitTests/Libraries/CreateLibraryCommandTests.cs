using Application.Data;
using Application.Libraries.Create;
using Ardalis.GuardClauses;
using Domain.Libraries;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Application.Interfaces;
using MongoDB.Bson;
using Persistence.Queries.Helpers;
using MockQueryable.NSubstitute;
using Persistence.Queries;
using Application.Libraries.ReadService;

namespace Application.UnitTests.Libraries;
public class CreateLibraryCommandTests
{
    private static readonly CreateLibraryCommand Command = new("test-name");

    private readonly CreateLibraryCommandHandler _handler;
    private readonly IRepository<Library, ObjectId> _librayRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ILibraryReadService _libraryReadServiceMock;

    public CreateLibraryCommandTests()
    {
        _librayRepositoryMock = Substitute.For<IRepository<Library, ObjectId>>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _libraryReadServiceMock = Substitute.For<ILibraryReadService>();

        _handler = new CreateLibraryCommandHandler(_librayRepositoryMock, _unitOfWorkMock, _libraryReadServiceMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess()
    {
        // Arrange
        _librayRepositoryMock.Add(Arg.Any<Library>());

        // Act
        var result = await _handler.Handle(Command, default);
        Guard.Against.Null(result.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(Command.Name);
        _librayRepositoryMock.Received(1).Add(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Should_ExecuteSaveChangeAsyncOnce()
    {
        // Arrange
        _librayRepositoryMock.Add(Arg.Any<Library>());

        // Act
        await _handler.Handle(Command, default);

        // Assert
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBadREquest_WhenCommandNameIsEmpty()
    {
        // Arrange
        CreateLibraryCommand Command = new("");

        // Act
        var result = await _handler.Handle(Command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesErrors.BadRequest);
    }

    [Fact]
    public async Task Handle_ShouldReturnDuplicate_WhenALibraryWithSameNameAlreadyExist()
    {
        // Arrange
        List<Library> list = [Library.Create(Command.Name)];
        var query = list.AsQueryable().BuildMock();
        var pagedList = new PagedList<Library>(query);
        await pagedList.ExecuteQueryAsync(1, 2);
        _libraryReadServiceMock.GetLibrariesAsync(Command.Name, LibrariesColumn.Name, null, 1, 1).Returns(pagedList);

        // Act
        var result = await _handler.Handle(Command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesErrors.Duplicate);
    }

}

using Application.Data;
using Domain.Libraries;
using FluentAssertions;
using NSubstitute;
using Application.Interfaces;
using MongoDB.Bson;
using Application.Libraries.Update;

namespace Application.UnitTests.Libraries;
public class UpdateLibraryCommandTests
{
    private static readonly UpdateLibraryCommand Command = new(new ObjectId(), "library");
    private static readonly Library library = Library.Create("library");

    private readonly UpdateLibraryCommandHandler _handler;
    private readonly IRepository<Library, ObjectId> _librayRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;

    public UpdateLibraryCommandTests()
    {
        _librayRepositoryMock = Substitute.For<IRepository<Library, ObjectId>>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();

        _handler = new UpdateLibraryCommandHandler(_librayRepositoryMock, _unitOfWorkMock);
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
        result.Error.Should().Be(LibrariesErrors.NotFound);
        _librayRepositoryMock.Received(0).Update(Arg.Any<Library>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(CancellationToken.None);
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
        _librayRepositoryMock.Received(1).Update(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
    }

}

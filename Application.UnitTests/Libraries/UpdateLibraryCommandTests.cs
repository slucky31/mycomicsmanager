using Application.Data;
using Domain.Libraries;
using FluentAssertions;
using NSubstitute;
using Application.Interfaces;
using MongoDB.Bson;
using Domain.Dto;
using Application.Librairies.Update;

namespace Application.UnitTests.Libraries;
public class UpdateLibraryCommandTests
{
    private static UpdateLibraryCommand Command = new(new LibraryId(new ObjectId()), "library");
    private static LibraryDto library = LibraryDto.Create(Library.Create("library"));

    private readonly UpdateLibraryCommandHandler _handler;
    private readonly IRepository<LibraryDto, LibraryId> _librayRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;

    public UpdateLibraryCommandTests()
    {
        _librayRepositoryMock = Substitute.For<IRepository<LibraryDto, LibraryId>>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();

        _handler = new UpdateLibraryCommandHandler(_librayRepositoryMock, _unitOfWorkMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenLibraryIsNotFound()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(Command.Id).Returns((LibraryDto?)null);

        // Act
        var result = await _handler.Handle(Command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesErrors.NotFound);
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
    }

}

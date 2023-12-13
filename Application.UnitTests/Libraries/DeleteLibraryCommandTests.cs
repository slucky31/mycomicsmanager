using Application.Data;
using Application.Librairies.Delete;
using Domain.Libraries;
using Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReceivedExtensions;

namespace Application.UnitTests.Libraries;
public class DeleteLibraryCommandTests
{
    private static DeleteLibraryCommand Command = new("test");
    private static Library library = Library.Create("test");

    private readonly DeleteLibraryCommandHandler _handler;
    private readonly IRepository<Library, string> _librayRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;

    public DeleteLibraryCommandTests()
    {
        _librayRepositoryMock = Substitute.For<IRepository<Library, string>>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();

        _handler = new DeleteLibraryCommandHandler(_librayRepositoryMock, _unitOfWorkMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenLibraryIsNull()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(Command.libraryId).Returns((Library?)null);

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
        _librayRepositoryMock.GetByIdAsync(Command.libraryId).Returns(library);

        // Act
        var result = await _handler.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

}

using Application.Interfaces;
using Application.Libraries.GetById;
using Ardalis.GuardClauses;
using Domain.Libraries;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using NSubstitute.ReturnsExtensions;

namespace Application.UnitTests.Libraries;
public class GetLibraryCommandTests
{

    private readonly GetLibraryQueryHandler _handler;
    private readonly IRepository<Library, Guid> _librayRepositoryMock;


    public GetLibraryCommandTests()
    {
        _librayRepositoryMock = Substitute.For<IRepository<Library, Guid>>();
        _handler = new GetLibraryQueryHandler(_librayRepositoryMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess()
    {
        // Arrange
        var library = Library.Create("test");
        var libraryId = library.Id;
        _librayRepositoryMock.GetByIdAsync(libraryId).Returns(library);
        var Query = new GetLibraryQuery(libraryId);

        // Act
        var result = await _handler.Handle(Query, CancellationToken.None);
        Guard.Against.Null(result.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(libraryId);
        result.Value.Name.Should().Be(library.Name);
        await _librayRepositoryMock.Received(1).GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenLibraryIsNotFound()
    {
        // Arrange
        var libraryId = Guid.CreateVersion7();
        _librayRepositoryMock.GetByIdAsync(libraryId).ReturnsNull();
        var Query = new GetLibraryQuery(libraryId);

        // Act
        var result = await _handler.Handle(Query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
        await _librayRepositoryMock.Received(1).GetByIdAsync(Arg.Any<Guid>());
    }



}

using Application.Interfaces;
using Application.Libraries;
using Application.Libraries.List;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Libraries;

public class GetLibrariesQueryHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly GetLibrariesQueryHandler _handler;
    private readonly ILibraryReadService _libraryReadServiceMock;

    public GetLibrariesQueryHandlerTests()
    {
        _libraryReadServiceMock = Substitute.For<ILibraryReadService>();
        _handler = new GetLibrariesQueryHandler(_libraryReadServiceMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnValidationError_WhenRequestIsNull()
    {
        // Act
        var result = await _handler.Handle(null!, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.ValidationError);
        await _libraryReadServiceMock.DidNotReceive().GetLibrariesAsync(
            Arg.Any<string?>(), Arg.Any<LibrariesColumn?>(), Arg.Any<SortOrder?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnPagedList_WhenRequestIsValid()
    {
        // Arrange
        var query = new GetLibrariesQuery(null, null, null, 1, 10, s_userId);
        var pagedList = Substitute.For<IPagedList<Library>>();
        _libraryReadServiceMock
            .GetLibrariesAsync(null, null, null, 1, 10, s_userId, CancellationToken.None)
            .Returns(pagedList);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(pagedList);
        await _libraryReadServiceMock.Received(1).GetLibrariesAsync(
            null, null, null, 1, 10, s_userId, CancellationToken.None);
    }
}

using Application.Libraries;
using Application.Libraries.Summaries;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.Libraries;

public class GetLibrarySummariesQueryHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly ILibraryReadService _libraryReadService = Substitute.For<ILibraryReadService>();
    private readonly GetLibrarySummariesQueryHandler _handler;

    public GetLibrarySummariesQueryHandlerTests()
    {
        _handler = new GetLibrarySummariesQueryHandler(_libraryReadService);
    }

    [Fact]
    public async Task Handle_Should_ReturnValidationError_WhenRequestIsNull()
    {
        // Act
        var result = await _handler.Handle(null!, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.ValidationError);
        await _libraryReadService.DidNotReceive().GetSummariesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnValidationError_WhenUserIdIsEmpty()
    {
        // Act
        var result = await _handler.Handle(new GetLibrarySummariesQuery(Guid.Empty), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.ValidationError);
        await _libraryReadService.DidNotReceive().GetSummariesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnTheUserLibrarySummaries_WhenRequestIsValid()
    {
        // Arrange
        IReadOnlyList<LibrarySummary> summaries =
        [
            new(Guid.CreateVersion7(), "Read Books", "#2E7D32", "Star", LibraryBookType.Physical, 24),
            new(Guid.CreateVersion7(), "À trier", "#5C6BC0", "Bookmark", LibraryBookType.Digital, 3),
        ];
        _libraryReadService.GetSummariesAsync(s_userId, Arg.Any<CancellationToken>()).Returns(summaries);

        // Act
        var result = await _handler.Handle(new GetLibrarySummariesQuery(s_userId), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(summaries);
    }
}

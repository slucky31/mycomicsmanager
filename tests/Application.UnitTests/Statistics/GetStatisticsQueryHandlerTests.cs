using Application.Interfaces;
using Application.Statistics.Get;
using Domain.Libraries;
using Domain.Statistics;
using NSubstitute;

namespace Application.UnitTests.Statistics;

public class GetStatisticsQueryHandlerTests
{
    private static readonly DateTimeOffset s_now = new(2026, 3, 15, 10, 30, 0, TimeSpan.Zero);
    private static readonly DateTime s_expectedFirstMonth = new(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly GetStatisticsQueryHandler _handler;
    private readonly IStatisticsReadService _statisticsReadServiceMock;
    private readonly IRepository<Library, Guid> _libraryRepositoryMock;

    public GetStatisticsQueryHandlerTests()
    {
        _statisticsReadServiceMock = Substitute.For<IStatisticsReadService>();
        _libraryRepositoryMock = Substitute.For<IRepository<Library, Guid>>();
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(s_now);

        _statisticsReadServiceMock
            .GetStatisticsAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new StatisticsDto());

        _handler = new GetStatisticsQueryHandler(_statisticsReadServiceMock, _libraryRepositoryMock, timeProvider);
    }

    private static Library CreateLibrary(Guid userId)
        => Library.Create("Test", "#FF0000", "book", LibraryBookType.Physical, userId).Value!;

    public static TheoryData<Guid, Guid?> InvalidIds => new()
    {
        { Guid.Empty, null },
        { Guid.CreateVersion7(), Guid.Empty }
    };

    [Theory]
    [MemberData(nameof(InvalidIds))]
    public async Task Handle_Should_ReturnBadRequest_WhenUserIdOrLibraryIdIsEmpty(Guid userId, Guid? libraryId)
    {
        // Arrange
        var query = new GetStatisticsQuery(userId, libraryId);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatisticsError.BadRequest);
        await _libraryRepositoryMock.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
        await _statisticsReadServiceMock.DidNotReceive()
            .GetStatisticsAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenLibraryDoesNotExist()
    {
        // Arrange
        var libraryId = Guid.CreateVersion7();
        var query = new GetStatisticsQuery(Guid.CreateVersion7(), libraryId);
        _libraryRepositoryMock.GetByIdAsync(libraryId).Returns((Library?)null);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
        await _statisticsReadServiceMock.DidNotReceive()
            .GetStatisticsAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenLibraryBelongsToOtherUser()
    {
        // Arrange
        var library = CreateLibrary(Guid.CreateVersion7());
        var query = new GetStatisticsQuery(Guid.CreateVersion7(), library.Id);
        _libraryRepositoryMock.GetByIdAsync(library.Id).Returns(library);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
        await _statisticsReadServiceMock.DidNotReceive()
            .GetStatisticsAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_QueryLibraryStatistics_WhenLibraryBelongsToUser()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var library = CreateLibrary(userId);
        var query = new GetStatisticsQuery(userId, library.Id);
        _libraryRepositoryMock.GetByIdAsync(library.Id).Returns(library);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _statisticsReadServiceMock.Received(1)
            .GetStatisticsAsync(userId, library.Id, s_expectedFirstMonth, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_QueryAllLibraries_WithoutCheckingLibrary_WhenLibraryIdIsNull()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var query = new GetStatisticsQuery(userId);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _libraryRepositoryMock.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
        await _statisticsReadServiceMock.Received(1)
            .GetStatisticsAsync(userId, null, s_expectedFirstMonth, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnTwelveMonthsWithZeroForMissingMonths_WhenSomeMonthsHaveNoReading()
    {
        // Arrange
        var statistics = new StatisticsDto
        {
            TotalBooks = 10,
            ReadBooks = 4,
            TotalReadings = 7,
            AverageRating = 4.5,
            ReadingsPerMonth =
            [
                new MonthlyReadingCountDto(2026, 3, 5),
                new MonthlyReadingCountDto(2025, 4, 2)
            ]
        };
        _statisticsReadServiceMock
            .GetStatisticsAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(statistics);

        // Act
        var result = await _handler.Handle(new GetStatisticsQuery(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var value = result.Value!;
        value.TotalBooks.Should().Be(10);
        value.UnreadBooks.Should().Be(6);
        value.AverageRating.Should().Be(4.5);
        value.ReadingsPerMonth.Should().HaveCount(GetStatisticsQueryHandler.MonthsInReadingHistory);
        value.ReadingsPerMonth[0].Should().Be(new MonthlyReadingCountDto(2025, 4, 2));
        value.ReadingsPerMonth[8].Should().Be(new MonthlyReadingCountDto(2025, 12, 0));
        value.ReadingsPerMonth[9].Should().Be(new MonthlyReadingCountDto(2026, 1, 0));
        value.ReadingsPerMonth[^1].Should().Be(new MonthlyReadingCountDto(2026, 3, 5));
        value.ReadingsPerMonth.Sum(m => m.Count).Should().Be(7);
    }
}

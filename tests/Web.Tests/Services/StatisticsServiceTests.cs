using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Statistics.Get;
using AwesomeAssertions;
using Domain.Primitives;
using Domain.Users;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class StatisticsServiceTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IQueryHandler<GetStatisticsQuery, StatisticsDto> _getStatisticsHandler;
    private readonly ICurrentUserService _currentUserService;
    private readonly StatisticsService _service;

    public StatisticsServiceTests()
    {
        _getStatisticsHandler = Substitute.For<IQueryHandler<GetStatisticsQuery, StatisticsDto>>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(s_userId);
        _service = new StatisticsService(_getStatisticsHandler, _currentUserService);
    }

    [Fact]
    public async Task Get_Should_ReturnError_WhenUserNotResolved()
    {
        // Arrange
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(Result<Guid>.Failure(UsersError.NotFound));

        // Act
        var result = await _service.Get(null, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UsersError.NotFound);
        await _getStatisticsHandler.DidNotReceive().Handle(Arg.Any<GetStatisticsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_Should_ForwardUserIdAndLibraryId_WhenUserResolved()
    {
        // Arrange
        var libraryId = Guid.CreateVersion7();
        var statistics = new StatisticsDto { TotalBooks = 3 };
        _getStatisticsHandler.Handle(Arg.Any<GetStatisticsQuery>(), Arg.Any<CancellationToken>()).Returns(statistics);

        // Act
        var result = await _service.Get(libraryId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(statistics);
        await _getStatisticsHandler.Received(1).Handle(
            new GetStatisticsQuery(s_userId, libraryId),
            Arg.Any<CancellationToken>());
    }
}

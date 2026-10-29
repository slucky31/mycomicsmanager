using Application.Abstractions.Messaging;
using Application.Books.Reading;
using Application.Interfaces;
using AwesomeAssertions;
using Domain.Primitives;
using Domain.Users;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class ReadingServiceTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IQueryHandler<GetBooksInProgressQuery, IReadOnlyList<BookInProgressDto>> _inProgressHandler =
        Substitute.For<IQueryHandler<GetBooksInProgressQuery, IReadOnlyList<BookInProgressDto>>>();
    private readonly IQueryHandler<GetRecentReadingsQuery, IPagedList<BookReadingDto>> _recentReadingsHandler =
        Substitute.For<IQueryHandler<GetRecentReadingsQuery, IPagedList<BookReadingDto>>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ReadingService _service;

    public ReadingServiceTests()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(s_userId);
        _service = new ReadingService(_inProgressHandler, _recentReadingsHandler, _currentUserService);
    }

    [Fact]
    public async Task GetInProgress_Should_ReturnError_WhenUserNotResolved()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(Result<Guid>.Failure(UsersError.NotFound));

        var result = await _service.GetInProgress(TestContext.Current.CancellationToken);

        result.Error.Should().Be(UsersError.NotFound);
        await _inProgressHandler.DidNotReceive().Handle(Arg.Any<GetBooksInProgressQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetInProgress_Should_AskForTheBooksOfTheCurrentUser_WhenUserResolved()
    {
        IReadOnlyList<BookInProgressDto> books = [];
        _inProgressHandler.Handle(new GetBooksInProgressQuery(s_userId), Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<BookInProgressDto>>.Success(books));

        var result = await _service.GetInProgress(TestContext.Current.CancellationToken);

        result.Value.Should().BeSameAs(books);
    }

    [Fact]
    public async Task GetRecentReadings_Should_ReturnError_WhenUserNotResolved()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(Result<Guid>.Failure(UsersError.NotFound));

        var result = await _service.GetRecentReadings(1, 20, TestContext.Current.CancellationToken);

        result.Error.Should().Be(UsersError.NotFound);
        await _recentReadingsHandler.DidNotReceive().Handle(Arg.Any<GetRecentReadingsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRecentReadings_Should_AskForThePageOfTheCurrentUser_WhenUserResolved()
    {
        var readings = Substitute.For<IPagedList<BookReadingDto>>();
        _recentReadingsHandler.Handle(new GetRecentReadingsQuery(s_userId, 2, 20), Arg.Any<CancellationToken>()).Returns(Result<IPagedList<BookReadingDto>>.Success(readings));

        var result = await _service.GetRecentReadings(2, 20, TestContext.Current.CancellationToken);

        result.Value.Should().BeSameAs(readings);
    }
}

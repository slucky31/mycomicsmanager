using Application.Abstractions.Messaging;
using Application.Books.ComicInfoSync;
using Application.Interfaces;
using AwesomeAssertions;
using Domain.Primitives;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class ComicInfoSyncServiceTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly ICommandHandler<StartComicInfoSyncCommand, int> _handler = Substitute.For<ICommandHandler<StartComicInfoSyncCommand, int>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ComicInfoSyncService _service;

    public ComicInfoSyncServiceTests()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(s_userId);
        _service = new ComicInfoSyncService(_handler, _currentUserService);
    }

    [Fact]
    public async Task StartAsync_Should_StartTheSyncForTheCurrentUser()
    {
        _handler.Handle(new StartComicInfoSyncCommand(s_userId), Arg.Any<CancellationToken>()).Returns(Result<int>.Success(12));

        var result = await _service.StartAsync(TestContext.Current.CancellationToken);

        result.Value.Should().Be(12);
    }

    [Fact]
    public async Task StartAsync_Should_NotStartTheSync_WhenTheUserIsUnknown()
    {
        var userError = new TError("USR404", "User not found");
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(Result<Guid>.Failure(userError));

        var result = await _service.StartAsync(TestContext.Current.CancellationToken);

        result.Error.Should().Be(userError);
        await _handler.DidNotReceive().Handle(Arg.Any<StartComicInfoSyncCommand>(), Arg.Any<CancellationToken>());
    }
}

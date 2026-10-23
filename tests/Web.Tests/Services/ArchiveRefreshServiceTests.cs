using Application.Abstractions.Messaging;
using Application.Books.ArchiveRefresh;
using Application.Interfaces;
using AwesomeAssertions;
using Domain.Primitives;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class ArchiveRefreshServiceTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly ICommandHandler<StartArchiveRefreshCommand, int> _handler = Substitute.For<ICommandHandler<StartArchiveRefreshCommand, int>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ArchiveRefreshService _service;

    public ArchiveRefreshServiceTests()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(s_userId);
        _service = new ArchiveRefreshService(_handler, _currentUserService);
    }

    [Fact]
    public async Task StartAsync_Should_StartTheRefreshForTheCurrentUser()
    {
        _handler.Handle(new StartArchiveRefreshCommand(s_userId), Arg.Any<CancellationToken>()).Returns(Result<int>.Success(12));

        var result = await _service.StartAsync(TestContext.Current.CancellationToken);

        result.Value.Should().Be(12);
    }

    [Fact]
    public async Task StartAsync_Should_NotStartTheRefresh_WhenTheUserIsUnknown()
    {
        var userError = new TError("USR404", "User not found");
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(Result<Guid>.Failure(userError));

        var result = await _service.StartAsync(TestContext.Current.CancellationToken);

        result.Error.Should().Be(userError);
        await _handler.DidNotReceive().Handle(Arg.Any<StartArchiveRefreshCommand>(), Arg.Any<CancellationToken>());
    }
}

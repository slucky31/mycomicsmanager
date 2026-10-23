using Application.Abstractions.Messaging;
using Application.ImportJobs.MigrateLegacy;
using Application.Interfaces;
using AwesomeAssertions;
using Domain.Users;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class LegacyMigrationServiceTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly ICommandHandler<StartLegacyMigrationCommand, int> _handler = Substitute.For<ICommandHandler<StartLegacyMigrationCommand, int>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly LegacyMigrationService _service;

    public LegacyMigrationServiceTests()
    {
        _service = new LegacyMigrationService(_handler, _currentUserService);
    }

    [Fact]
    public async Task StartAsync_Should_StartTheMigrationForTheCurrentUser()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(s_userId);
        _handler.Handle(new StartLegacyMigrationCommand(20, s_userId), Arg.Any<CancellationToken>()).Returns(20);

        var result = await _service.StartAsync(20, TestContext.Current.CancellationToken);

        result.Value.Should().Be(20);
    }

    [Fact]
    public async Task StartAsync_Should_ReturnTheError_WhenTheUserIsUnknown()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(UsersError.NotFound);

        var result = await _service.StartAsync(20, TestContext.Current.CancellationToken);

        result.Error.Should().Be(UsersError.NotFound);
        await _handler.DidNotReceive().Handle(Arg.Any<StartLegacyMigrationCommand>(), Arg.Any<CancellationToken>());
    }
}

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

    private readonly ICommandHandler<MigrateLegacyBooksCommand, MigrateLegacyBooksResult> _handler = Substitute.For<ICommandHandler<MigrateLegacyBooksCommand, MigrateLegacyBooksResult>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly LegacyMigrationService _service;

    public LegacyMigrationServiceTests()
    {
        _service = new LegacyMigrationService(_handler, _currentUserService);
    }

    [Fact]
    public async Task MigrateAsync_Should_MigrateForTheCurrentUser()
    {
        var moved = new MigrateLegacyBooksResult(20, 0, 0, 80);
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(s_userId);
        _handler.Handle(new MigrateLegacyBooksCommand(20, s_userId), Arg.Any<CancellationToken>()).Returns(moved);

        var result = await _service.MigrateAsync(20, TestContext.Current.CancellationToken);

        result.Value.Should().Be(moved);
    }

    [Fact]
    public async Task MigrateAsync_Should_ReturnTheError_WhenTheUserIsUnknown()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(UsersError.NotFound);

        var result = await _service.MigrateAsync(20, TestContext.Current.CancellationToken);

        result.Error.Should().Be(UsersError.NotFound);
        await _handler.DidNotReceive().Handle(Arg.Any<MigrateLegacyBooksCommand>(), Arg.Any<CancellationToken>());
    }
}

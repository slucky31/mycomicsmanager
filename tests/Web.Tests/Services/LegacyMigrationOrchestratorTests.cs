using Application.Abstractions.Messaging;
using Application.ImportJobs.MigrateLegacy;
using AwesomeAssertions;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class LegacyMigrationOrchestratorTests
{
    private readonly ICommandHandler<MigrateLegacyBooksCommand, MigrateLegacyBooksResult> _handler =
        Substitute.For<ICommandHandler<MigrateLegacyBooksCommand, MigrateLegacyBooksResult>>();
    private readonly LegacyMigrationOrchestrator _orchestrator;

    public LegacyMigrationOrchestratorTests()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICommandHandler<MigrateLegacyBooksCommand, MigrateLegacyBooksResult>)).Returns(_handler);
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        _orchestrator = new LegacyMigrationOrchestrator(scopeFactory, NullLogger<LegacyMigrationOrchestrator>.Instance);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MigrateAsync_Should_MoveTheFilesOfTheUserInItsOwnScope(bool succeeds)
    {
        var userId = Guid.CreateVersion7();
        _handler.Handle(Arg.Any<MigrateLegacyBooksCommand>(), Arg.Any<CancellationToken>())
            .Returns(succeeds
                ? Result<MigrateLegacyBooksResult>.Success(new MigrateLegacyBooksResult(18, 1, 1, 80))
                : Result<MigrateLegacyBooksResult>.Failure(new TError("LegacyMigration.SourceEmpty", "Empty")));

        var act = () => _orchestrator.MigrateAsync(20, userId, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        await _handler.Received(1).Handle(new MigrateLegacyBooksCommand(20, userId), Arg.Any<CancellationToken>());
    }
}

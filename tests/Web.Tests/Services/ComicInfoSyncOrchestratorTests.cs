using Application.Abstractions.Messaging;
using Application.Books.ComicInfoSync;
using AwesomeAssertions;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class ComicInfoSyncOrchestratorTests
{
    private readonly ICommandHandler<SyncComicInfoCommand, ComicInfoSyncSummary> _handler =
        Substitute.For<ICommandHandler<SyncComicInfoCommand, ComicInfoSyncSummary>>();
    private readonly ComicInfoSyncOrchestrator _orchestrator;

    public ComicInfoSyncOrchestratorTests()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICommandHandler<SyncComicInfoCommand, ComicInfoSyncSummary>)).Returns(_handler);
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        _orchestrator = new ComicInfoSyncOrchestrator(scopeFactory, NullLogger<ComicInfoSyncOrchestrator>.Instance);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SyncAsync_Should_SyncTheBooksOfTheUserInItsOwnScope(bool succeeds)
    {
        var userId = Guid.CreateVersion7();
        _handler.Handle(Arg.Any<SyncComicInfoCommand>(), Arg.Any<CancellationToken>())
            .Returns(succeeds
                ? Result<ComicInfoSyncSummary>.Success(new ComicInfoSyncSummary(3, 1))
                : Result<ComicInfoSyncSummary>.Failure(new TError("BOK400", "Bad request")));

        var act = () => _orchestrator.SyncAsync(userId, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        await _handler.Received(1).Handle(new SyncComicInfoCommand(userId), Arg.Any<CancellationToken>());
    }
}

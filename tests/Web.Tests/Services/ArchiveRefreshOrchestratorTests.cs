using Application.Abstractions.Messaging;
using Application.Books.ArchiveRefresh;
using AwesomeAssertions;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class ArchiveRefreshOrchestratorTests
{
    private readonly ICommandHandler<RefreshFromArchiveCommand, ArchiveRefreshSummary> _handler =
        Substitute.For<ICommandHandler<RefreshFromArchiveCommand, ArchiveRefreshSummary>>();
    private readonly ArchiveRefreshOrchestrator _orchestrator;

    public ArchiveRefreshOrchestratorTests()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICommandHandler<RefreshFromArchiveCommand, ArchiveRefreshSummary>)).Returns(_handler);
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        _orchestrator = new ArchiveRefreshOrchestrator(scopeFactory, NullLogger<ArchiveRefreshOrchestrator>.Instance);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RefreshAsync_Should_RefreshTheBooksOfTheUserInItsOwnScope(bool succeeds)
    {
        var userId = Guid.CreateVersion7();
        _handler.Handle(Arg.Any<RefreshFromArchiveCommand>(), Arg.Any<CancellationToken>())
            .Returns(succeeds
                ? Result<ArchiveRefreshSummary>.Success(new ArchiveRefreshSummary(3, 2, 1))
                : Result<ArchiveRefreshSummary>.Failure(new TError("BOK400", "Bad request")));

        var act = () => _orchestrator.RefreshAsync(userId, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        await _handler.Received(1).Handle(new RefreshFromArchiveCommand(userId), Arg.Any<CancellationToken>());
    }
}

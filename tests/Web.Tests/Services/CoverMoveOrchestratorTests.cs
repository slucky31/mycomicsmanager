using Application.Abstractions.Messaging;
using Application.Books.Covers;
using AwesomeAssertions;
using Domain.Books;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class CoverMoveOrchestratorTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MoveAsync_Should_MoveTheCoversInItsOwnScope(bool succeeds)
    {
        var handler = Substitute.For<ICommandHandler<MoveCoversCommand, CoverMoveSummary>>();
        handler.Handle(Arg.Any<MoveCoversCommand>(), Arg.Any<CancellationToken>())
            .Returns(succeeds ? Result.Success(new CoverMoveSummary(3, 1)) : Result.Failure<CoverMoveSummary>(BooksError.CoversNotListed));
        var services = new ServiceCollection().AddSingleton(handler).BuildServiceProvider();
        await using var _ = services;
        var orchestrator = new CoverMoveOrchestrator(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<CoverMoveOrchestrator>.Instance);

        var act = () => orchestrator.MoveAsync(TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        await handler.Received(1).Handle(Arg.Any<MoveCoversCommand>(), Arg.Any<CancellationToken>());
    }
}

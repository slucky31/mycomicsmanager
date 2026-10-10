using Application.Abstractions.Messaging;
using Application.Books.IsbnScan;
using AwesomeAssertions;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class IsbnScanOrchestratorTests
{
    private readonly ICommandHandler<ScanLibraryIsbnsCommand, LibraryIsbnScanSummary> _handler =
        Substitute.For<ICommandHandler<ScanLibraryIsbnsCommand, LibraryIsbnScanSummary>>();
    private readonly IsbnScanOrchestrator _orchestrator;

    public IsbnScanOrchestratorTests()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICommandHandler<ScanLibraryIsbnsCommand, LibraryIsbnScanSummary>)).Returns(_handler);
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        _orchestrator = new IsbnScanOrchestrator(scopeFactory, NullLogger<IsbnScanOrchestrator>.Instance);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ScanLibraryAsync_Should_ScanTheLibraryInItsOwnScope(bool succeeds)
    {
        var libraryId = Guid.CreateVersion7();
        _handler.Handle(Arg.Any<ScanLibraryIsbnsCommand>(), Arg.Any<CancellationToken>())
            .Returns(succeeds
                ? Result<LibraryIsbnScanSummary>.Success(new LibraryIsbnScanSummary(1, 2, 3, 4))
                : Result<LibraryIsbnScanSummary>.Failure(new TError("LIB400", "Bad request")));

        var act = () => _orchestrator.ScanLibraryAsync(libraryId, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        await _handler.Received(1).Handle(new ScanLibraryIsbnsCommand(libraryId), Arg.Any<CancellationToken>());
    }
}

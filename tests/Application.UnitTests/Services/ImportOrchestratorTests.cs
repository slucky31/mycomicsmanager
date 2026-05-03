using Application.Abstractions.Messaging;
using Application.ImportJobs.Process;
using Domain.Books;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Persistence.Services;

namespace Application.UnitTests.Services;

public sealed class ImportOrchestratorTests
{
    private readonly ICommandHandler<ProcessImportJobCommand, DigitalBook> _commandHandler;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ImportOrchestrator _orchestrator;

    public ImportOrchestratorTests()
    {
        _commandHandler = Substitute.For<ICommandHandler<ProcessImportJobCommand, DigitalBook>>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICommandHandler<ProcessImportJobCommand, DigitalBook>))
            .Returns(_commandHandler);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        // CreateAsyncScope() is an extension method that calls CreateScope() internally —
        // mock CreateScope() only; the real extension will wrap it in AsyncServiceScope.
        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scopeFactory.CreateScope().Returns(scope);

        _orchestrator = new ImportOrchestrator(_scopeFactory);
    }

    [Fact]
    public async Task ProcessAsync_Should_CallProcessImportJobCommandHandler()
    {
        var importJobId = Guid.CreateVersion7();
        _commandHandler.Handle(Arg.Any<ProcessImportJobCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<DigitalBook>.Failure(new TError("TEST", "test")));

        await _orchestrator.ProcessAsync(importJobId, TestContext.Current.CancellationToken);

        await _commandHandler.Received(1).Handle(
            Arg.Is<ProcessImportJobCommand>(c => c.ImportJobId == importJobId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_Should_CompleteWithoutException_WhenHandlerReturnsFailure()
    {
        var importJobId = Guid.CreateVersion7();
        _commandHandler.Handle(Arg.Any<ProcessImportJobCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<DigitalBook>.Failure(new TError("FAIL", "Job failed")));

        var act = async () => await _orchestrator.ProcessAsync(importJobId, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        await _commandHandler.Received(1).Handle(
            Arg.Is<ProcessImportJobCommand>(c => c.ImportJobId == importJobId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_Should_RethrowException_WhenHandlerThrows()
    {
        var importJobId = Guid.CreateVersion7();
        _commandHandler.Handle(Arg.Any<ProcessImportJobCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Unexpected failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _orchestrator.ProcessAsync(importJobId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ProcessAsync_Should_CreateNewScope_ForEachCall()
    {
        var importJobId = Guid.CreateVersion7();
        _commandHandler.Handle(Arg.Any<ProcessImportJobCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<DigitalBook>.Failure(new TError("FAIL", "fail")));

        await _orchestrator.ProcessAsync(importJobId, TestContext.Current.CancellationToken);
        await _orchestrator.ProcessAsync(importJobId, TestContext.Current.CancellationToken);

        // CreateAsyncScope() delegates to CreateScope() — verify the underlying call
        _scopeFactory.Received(2).CreateScope();
    }
}

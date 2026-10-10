using Application.Abstractions.Messaging;
using Application.Books.IsbnScan;
using Application.Interfaces;
using AwesomeAssertions;
using Domain.Primitives;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class IsbnScanServiceTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly ICommandHandler<StartLibraryIsbnScanCommand, int> _handler = Substitute.For<ICommandHandler<StartLibraryIsbnScanCommand, int>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IsbnScanService _service;

    public IsbnScanServiceTests()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(s_userId);
        _service = new IsbnScanService(_handler, _currentUserService);
    }

    [Fact]
    public async Task StartLibraryScanAsync_Should_StartTheScanForTheCurrentUser()
    {
        var libraryId = Guid.CreateVersion7();
        _handler.Handle(new StartLibraryIsbnScanCommand(libraryId, s_userId), Arg.Any<CancellationToken>()).Returns(Result<int>.Success(4));

        var result = await _service.StartLibraryScanAsync(libraryId, TestContext.Current.CancellationToken);

        result.Value.Should().Be(4);
    }

    [Fact]
    public async Task StartLibraryScanAsync_Should_NotStartTheScan_WhenTheUserIsUnknown()
    {
        var userError = new TError("USR404", "User not found");
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(Result<Guid>.Failure(userError));

        var result = await _service.StartLibraryScanAsync(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(userError);
        await _handler.DidNotReceive().Handle(Arg.Any<StartLibraryIsbnScanCommand>(), Arg.Any<CancellationToken>());
    }
}

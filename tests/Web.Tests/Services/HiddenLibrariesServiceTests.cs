using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Users.HiddenLibraries;
using AwesomeAssertions;
using Domain.Primitives;
using Domain.Users;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class HiddenLibrariesServiceTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IQueryHandler<GetShowHiddenLibrariesQuery, bool> _getHandler = Substitute.For<IQueryHandler<GetShowHiddenLibrariesQuery, bool>>();
    private readonly ICommandHandler<SetShowHiddenLibrariesCommand> _setHandler = Substitute.For<ICommandHandler<SetShowHiddenLibrariesCommand>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly HiddenLibrariesService _service;

    public HiddenLibrariesServiceTests()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(s_userId);
        _service = new HiddenLibrariesService(_getHandler, _setHandler, _currentUserService);
    }

    [Fact]
    public async Task GetShowHiddenLibraries_Should_QueryTheSettingOfTheCurrentUser()
    {
        // Arrange
        _getHandler.Handle(Arg.Any<GetShowHiddenLibrariesQuery>(), Arg.Any<CancellationToken>()).Returns(Result<bool>.Success(true));

        // Act
        var result = await _service.GetShowHiddenLibraries(TestContext.Current.CancellationToken);

        // Assert
        result.Value.Should().BeTrue();
        await _getHandler.Received(1).Handle(new GetShowHiddenLibrariesQuery(s_userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetShowHiddenLibraries_Should_SaveTheSettingOfTheCurrentUser()
    {
        // Arrange
        _setHandler.Handle(Arg.Any<SetShowHiddenLibrariesCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        // Act
        var result = await _service.SetShowHiddenLibraries(true, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _setHandler.Received(1).Handle(new SetShowHiddenLibrariesCommand(s_userId, true), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShowHiddenLibraries_Should_ReturnTheUserError_WhenTheCurrentUserIsUnknown()
    {
        // Arrange
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(Result<Guid>.Failure(UsersError.NotFound));

        // Act
        var read = await _service.GetShowHiddenLibraries(TestContext.Current.CancellationToken);
        var saved = await _service.SetShowHiddenLibraries(true, TestContext.Current.CancellationToken);

        // Assert
        read.Error.Should().Be(UsersError.NotFound);
        saved.Error.Should().Be(UsersError.NotFound);
        await _getHandler.DidNotReceive().Handle(Arg.Any<GetShowHiddenLibrariesQuery>(), Arg.Any<CancellationToken>());
        await _setHandler.DidNotReceive().Handle(Arg.Any<SetShowHiddenLibrariesCommand>(), Arg.Any<CancellationToken>());
    }
}

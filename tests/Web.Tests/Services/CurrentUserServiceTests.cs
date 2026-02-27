using System.Security.Claims;
using Application.Users;
using AwesomeAssertions;
using Domain.Primitives;
using Domain.Users;
using Microsoft.AspNetCore.Components.Authorization;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class CurrentUserServiceTests
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IUserReadService _userReadService;
    private readonly CurrentUserService _service;

    public CurrentUserServiceTests()
    {
        _authStateProvider = Substitute.For<AuthenticationStateProvider>();
        _userReadService = Substitute.For<IUserReadService>();
        _service = new CurrentUserService(_authStateProvider, _userReadService);
    }

    private static AuthenticationState CreateAuthState(string? sub)
    {
        if (string.IsNullOrEmpty(sub))
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            return new AuthenticationState(anonymous);
        }

        var identity = new ClaimsIdentity(
            [new Claim("sub", sub)],
            authenticationType: "test");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    [Fact]
    public async Task CurrentUserService_Should_ReturnUserId_WhenUserAuthenticated()
    {
        // Arrange
        const string sub = "auth0|user123";
        var user = User.Create("user@example.com", sub);
        _authStateProvider.GetAuthenticationStateAsync().Returns(CreateAuthState(sub));
        _userReadService.GetUserByAuthId(sub).Returns(user);

        // Act
        var result = await _service.GetCurrentUserIdAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(user.Id);
    }

    [Fact]
    public async Task CurrentUserService_Should_ReturnNotFound_WhenUserNotAuthenticated()
    {
        // Arrange
        _authStateProvider.GetAuthenticationStateAsync().Returns(CreateAuthState(null));

        // Act
        var result = await _service.GetCurrentUserIdAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UsersError.NotFound);
        await _userReadService.DidNotReceive().GetUserByAuthId(Arg.Any<string>());
    }

    [Fact]
    public async Task CurrentUserService_Should_ReturnError_WhenUserNotFoundInDatabase()
    {
        // Arrange
        const string sub = "auth0|unknown";
        _authStateProvider.GetAuthenticationStateAsync().Returns(CreateAuthState(sub));
        _userReadService.GetUserByAuthId(sub).Returns(Result<User>.Failure(UsersError.NotFound));

        // Act
        var result = await _service.GetCurrentUserIdAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UsersError.NotFound);
    }

    [Fact]
    public async Task CurrentUserService_Should_CallUserReadService_WhenSubIsPresent()
    {
        // Arrange
        const string sub = "auth0|test123";
        var user = User.Create("test@example.com", sub);
        _authStateProvider.GetAuthenticationStateAsync().Returns(CreateAuthState(sub));
        _userReadService.GetUserByAuthId(sub).Returns(user);

        // Act
        await _service.GetCurrentUserIdAsync();

        // Assert
        await _userReadService.Received(1).GetUserByAuthId(sub);
    }
}

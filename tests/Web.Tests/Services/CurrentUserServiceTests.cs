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

    private static AuthenticationState CreateAuthState(string? sub, string? email = null)
    {
        if (string.IsNullOrEmpty(sub) && string.IsNullOrEmpty(email))
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            return new AuthenticationState(anonymous);
        }

        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(sub))
        {
            claims.Add(new Claim("sub", sub));
        }

        if (!string.IsNullOrEmpty(email))
        {
            claims.Add(new Claim(ClaimTypes.Name, email));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "test");
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
        await _userReadService.DidNotReceive().GetUserByEmail(Arg.Any<string>());
    }

    [Fact]
    public async Task CurrentUserService_Should_ReturnError_WhenUserNotFoundBySubOrEmail()
    {
        // Arrange — sub present but not in DB, no email claim either
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
    public async Task CurrentUserService_Should_FallbackToEmail_WhenSubNotFoundInDatabase()
    {
        // Arrange — sub present but not in DB; email present and in DB (pre-migration user)
        const string sub = "auth0|user456";
        const string email = "existing@example.com";
        var user = User.Create(email, "old-sid");
        _authStateProvider.GetAuthenticationStateAsync().Returns(CreateAuthState(sub, email));
        _userReadService.GetUserByAuthId(sub).Returns(Result<User>.Failure(UsersError.NotFound));
        _userReadService.GetUserByEmail(email).Returns(user);

        // Act
        var result = await _service.GetCurrentUserIdAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(user.Id);
    }

    [Fact]
    public async Task CurrentUserService_Should_ReturnError_WhenEmailLookupFails()
    {
        // Arrange — sub not in DB, email present but GetUserByEmail also fails
        const string sub = "auth0|unknown";
        const string email = "unknown@example.com";
        _authStateProvider.GetAuthenticationStateAsync().Returns(CreateAuthState(sub, email));
        _userReadService.GetUserByAuthId(sub).Returns(Result<User>.Failure(UsersError.NotFound));
        _userReadService.GetUserByEmail(email).Returns(Result<User>.Failure(UsersError.NotFound));

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

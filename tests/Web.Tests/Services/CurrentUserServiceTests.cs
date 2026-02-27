using Application.Interfaces;
using Application.Users;
using AwesomeAssertions;
using Domain.Primitives;
using Domain.Users;
using Microsoft.AspNetCore.Components.Authorization;
using NSubstitute;
using System.Security.Claims;
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

    private static AuthenticationState CreateAuthState(string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            return new AuthenticationState(anonymous);
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, email)],
            authenticationType: "test");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    [Fact]
    public async Task CurrentUserService_Should_ReturnUserId_WhenUserAuthenticated()
    {
        // Arrange
        const string email = "user@example.com";
        var userId = Guid.CreateVersion7();
        var user = User.Create(email, "auth-id-123");
        _authStateProvider.GetAuthenticationStateAsync().Returns(CreateAuthState(email));
        _userReadService.GetUserByEmail(email).Returns(user);

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
        await _userReadService.DidNotReceive().GetUserByEmail(Arg.Any<string>());
    }

    [Fact]
    public async Task CurrentUserService_Should_ReturnError_WhenUserEmailNotFoundInDatabase()
    {
        // Arrange
        const string email = "unknown@example.com";
        _authStateProvider.GetAuthenticationStateAsync().Returns(CreateAuthState(email));
        _userReadService.GetUserByEmail(email).Returns(Result<User>.Failure(UsersError.NotFound));

        // Act
        var result = await _service.GetCurrentUserIdAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UsersError.NotFound);
    }

    [Fact]
    public async Task CurrentUserService_Should_CallUserReadService_WhenEmailIsPresent()
    {
        // Arrange
        const string email = "test@example.com";
        var user = User.Create(email, "auth-sid");
        _authStateProvider.GetAuthenticationStateAsync().Returns(CreateAuthState(email));
        _userReadService.GetUserByEmail(email).Returns(user);

        // Act
        await _service.GetCurrentUserIdAsync();

        // Assert
        await _userReadService.Received(1).GetUserByEmail(email);
    }
}

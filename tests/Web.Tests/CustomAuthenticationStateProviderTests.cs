using System.Security.Claims;
using Application.Interfaces;
using Application.Users;
using AwesomeAssertions;
using Domain.Primitives;
using Domain.Users;
using Microsoft.AspNetCore.Components.Authorization;
using NSubstitute;
using Web.Tests.Services;
using Xunit;

namespace Web.Tests;

public sealed class CustomAuthenticationStateProviderTests
{
    private readonly IUserReadService _userReadService = Substitute.For<IUserReadService>();
    private readonly IRepository<User, Guid> _userRepository = Substitute.For<IRepository<User, Guid>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public CustomAuthenticationStateProviderTests()
    {
        _userReadService.GetUserByAuthId(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<User>(UsersError.NotFound));
        _userReadService.GetUserByEmail(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<User>(UsersError.NotFound));
    }

    private async Task SignInAsync(ClaimsPrincipal principal)
    {
        var provider = new CustomAuthenticationStateProvider(_userReadService, _userRepository, _unitOfWork);
        provider.SetAuthenticationState(Task.FromResult(new AuthenticationState(principal)));
        await provider.GetAuthenticationStateAsync();
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_Should_QueryUserOnce_WhenCalledConcurrently()
    {
        var pendingLookup = new TaskCompletionSource<Result<User>>();
        var userReadService = Substitute.For<IUserReadService>();
        userReadService.GetUserByAuthId("auth0|42", Arg.Any<CancellationToken>()).Returns(pendingLookup.Task);
        var provider = new CustomAuthenticationStateProvider(
            userReadService, Substitute.For<IRepository<User, Guid>>(), Substitute.For<IUnitOfWork>());
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "auth0|42")], "test"));
        provider.SetAuthenticationState(Task.FromResult(new AuthenticationState(principal)));

        var first = provider.GetAuthenticationStateAsync();
        var second = provider.GetAuthenticationStateAsync();
        pendingLookup.SetResult(User.Create("user@example.com", "auth0|42"));
        await Task.WhenAll(first, second);

        await userReadService.Received(1).GetUserByAuthId("auth0|42", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_Should_LinkTheSubToTheLegacyUser_WhenItsEmailIsVerified()
    {
        var legacy = User.Create("reader@example.com", "");
        _userReadService.GetUserByEmail("reader@example.com", Arg.Any<CancellationToken>()).Returns(legacy);
        _userRepository.GetByIdAsync(legacy.Id).Returns(legacy);

        await SignInAsync(UserIdentityTests.Principal("auth0|42", "reader@example.com"));

        legacy.AuthId.Should().Be("auth0|42");
        legacy.Email.Should().Be("reader@example.com");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Theory]
    [InlineData("auth0|victim", "true")]
    [InlineData("", "false")]
    public async Task GetAuthenticationStateAsync_Should_NeitherLinkNorCreate_WhenTheEmailIsNotProvenForThatUser(string existingAuthId, string emailVerified)
    {
        var existing = User.Create("reader@example.com", existingAuthId);
        _userReadService.GetUserByEmail("reader@example.com", Arg.Any<CancellationToken>()).Returns(existing);

        await SignInAsync(UserIdentityTests.Principal("auth0|attacker", "reader@example.com", emailVerified));

        existing.AuthId.Should().Be(existingAuthId);
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_Should_CreateTheUser_WhenTheEmailIsUnknown()
    {
        await SignInAsync(UserIdentityTests.Principal("auth0|42", "new@example.com", emailVerified: null));

        _userRepository.Received(1).Add(Arg.Is<User>(u => u.AuthId == "auth0|42" && u.Email == "new@example.com"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_Should_NotProvision_WhenThereIsNoEmail()
    {
        await SignInAsync(UserIdentityTests.Principal("auth0|42"));

        await _userReadService.DidNotReceive().GetUserByEmail(Arg.Any<string?>(), Arg.Any<CancellationToken>());
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }
}

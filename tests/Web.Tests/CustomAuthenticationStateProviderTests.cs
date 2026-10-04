using System.Security.Claims;
using Application.Interfaces;
using Application.Users;
using Domain.Primitives;
using Domain.Users;
using Microsoft.AspNetCore.Components.Authorization;
using NSubstitute;
using Xunit;

namespace Web.Tests;

public sealed class CustomAuthenticationStateProviderTests
{
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
}

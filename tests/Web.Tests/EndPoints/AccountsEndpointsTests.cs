using Auth0.AspNetCore.Authentication;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Web.EndPoints;
using Xunit;

namespace Web.Tests.EndPoints;

public sealed class AccountsEndpointsTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/books/42")]
    [InlineData("/libraries?view=grid#top")]
    public void ToLocalPath_Should_KeepThePath_WhenItIsLocal(string target)
    {
        AccountsEndpoints.ToLocalPath(target).Should().Be(target);
    }

    [Theory]
    [InlineData("https://example.invalid/phish")]
    [InlineData("//example.invalid")]
    [InlineData("/\\example.invalid")]
    [InlineData("/\t/example.invalid")]
    [InlineData("books")]
    [InlineData("")]
    [InlineData(null)]
    public void ToLocalPath_Should_ReturnTheRoot_WhenTheUriIsNotALocalPath(string? target)
    {
        AccountsEndpoints.ToLocalPath(target).Should().Be("/");
    }

    [Fact]
    public async Task LoginAsync_Should_ReturnToTheRoot_WhenTheRedirectUriIsAnotherSite()
    {
        var (context, authentication) = CreateContext();

        await AccountsEndpoints.LoginAsync(context, "https://example.invalid/phish");

        await authentication.Received(1).ChallengeAsync(context, Auth0Constants.AuthenticationScheme,
            Arg.Is<AuthenticationProperties>(p => p.RedirectUri == "/" && p.IsPersistent));
    }

    [Fact]
    public async Task LogoutAsync_Should_ReturnToTheRoot_WhenTheRedirectUriIsAnotherSite()
    {
        var (context, authentication) = CreateContext();

        await AccountsEndpoints.LogoutAsync(context, "//example.invalid");

        await authentication.Received(1).SignOutAsync(context, Auth0Constants.AuthenticationScheme,
            Arg.Is<AuthenticationProperties>(p => p.RedirectUri == "/"));
    }

    private static (DefaultHttpContext Context, IAuthenticationService Authentication) CreateContext()
    {
        var authentication = Substitute.For<IAuthenticationService>();
        var services = new ServiceCollection().AddSingleton(authentication).BuildServiceProvider();
        return (new DefaultHttpContext { RequestServices = services }, authentication);
    }
}

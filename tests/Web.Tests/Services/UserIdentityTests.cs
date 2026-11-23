using System.Security.Claims;
using Application.Users;
using AwesomeAssertions;
using Domain.Primitives;
using Domain.Users;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class UserIdentityTests
{
    private const string Email = "reader@example.com";

    private readonly IUserReadService _userReadService = Substitute.For<IUserReadService>();

    public UserIdentityTests()
    {
        _userReadService.GetUserByAuthId(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<User>(UsersError.NotFound));
        _userReadService.GetUserByEmail(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<User>(UsersError.NotFound));
    }

    internal static ClaimsPrincipal Principal(string? sub, string? email = null, string? emailVerified = "true", string? name = null)
    {
        var claims = new List<Claim>();
        if (sub is not null)
        {
            claims.Add(new Claim("sub", sub));
        }
        if (email is not null)
        {
            claims.Add(new Claim("email", email));
        }
        if (emailVerified is not null)
        {
            claims.Add(new Claim("email_verified", emailVerified));
        }
        if (name is not null)
        {
            claims.Add(new Claim(ClaimTypes.Name, name));
        }
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public async Task ResolveUserIdAsync_Should_ReturnTheUserOfTheSub()
    {
        var user = User.Create(Email, "auth0|42");
        _userReadService.GetUserByAuthId("auth0|42", Arg.Any<CancellationToken>()).Returns(user);

        var userId = await UserIdentity.ResolveUserIdAsync(Principal("auth0|42"), _userReadService, TestContext.Current.CancellationToken);

        userId.Should().Be(user.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("old-sid")]
    public async Task ResolveUserIdAsync_Should_ReturnTheLegacyUser_WhenItsEmailIsVerified(string legacyAuthId)
    {
        var legacy = User.Create(Email, legacyAuthId);
        _userReadService.GetUserByEmail(Email, Arg.Any<CancellationToken>()).Returns(legacy);

        var userId = await UserIdentity.ResolveUserIdAsync(Principal("auth0|new", Email), _userReadService, TestContext.Current.CancellationToken);

        userId.Should().Be(legacy.Id);
    }

    [Fact]
    public async Task ResolveUserIdAsync_Should_ReturnNull_WhenTheNameIsTheEmailOfALinkedUser()
    {
        // The audit case: a second Auth0 account whose "name" is the e-mail of the victim.
        var victim = User.Create(Email, "auth0|victim");
        _userReadService.GetUserByEmail(Email, Arg.Any<CancellationToken>()).Returns(victim);

        var userId = await UserIdentity.ResolveUserIdAsync(
            Principal("auth0|attacker", "attacker@example.com", name: Email), _userReadService, TestContext.Current.CancellationToken);

        userId.Should().BeNull();
        await _userReadService.DidNotReceive().GetUserByEmail(Email, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveUserIdAsync_Should_ReturnNull_WhenTheVerifiedEmailBelongsToAnotherSub()
    {
        _userReadService.GetUserByEmail(Email, Arg.Any<CancellationToken>()).Returns(User.Create(Email, "auth0|victim"));

        var userId = await UserIdentity.ResolveUserIdAsync(Principal("auth0|attacker", Email), _userReadService, TestContext.Current.CancellationToken);

        userId.Should().BeNull();
    }

    [Theory]
    [InlineData("false")]
    [InlineData(null)]
    [InlineData("yes")]
    public async Task ResolveUserIdAsync_Should_IgnoreTheEmail_WhenAuth0HasNotVerifiedIt(string? emailVerified)
    {
        _userReadService.GetUserByEmail(Email, Arg.Any<CancellationToken>()).Returns(User.Create(Email, ""));

        var userId = await UserIdentity.ResolveUserIdAsync(Principal("auth0|new", Email, emailVerified), _userReadService, TestContext.Current.CancellationToken);

        userId.Should().BeNull();
        await _userReadService.DidNotReceive().GetUserByEmail(Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveUserIdAsync_Should_ReturnNull_WhenTheVerifiedEmailIsUnknown()
    {
        var userId = await UserIdentity.ResolveUserIdAsync(Principal(null, Email), _userReadService, TestContext.Current.CancellationToken);

        userId.Should().BeNull();
    }

    [Fact]
    public void GetVerifiedEmail_Should_ReadTheMappedEmailClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, Email), new Claim("email_verified", "True")], "test"));

        UserIdentity.GetVerifiedEmail(principal).Should().Be(Email);
    }

    [Fact]
    public void GetSub_Should_ReadTheMappedNameIdentifier_WhenThereIsNoSubClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "auth0|42")], "test"));

        UserIdentity.GetSub(principal).Should().Be("auth0|42");
    }
}

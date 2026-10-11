using Base.Integration.Tests;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Persistence.Tests.Integration.Authentication;

[Collection("DatabaseCollectionTests")]
public class AuthenticationPersistenceTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public void CookieAuthenticationOptions_Should_KeepUsersSignedInForSevenDays()
    {
        var options = _scope.ServiceProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        options.ExpireTimeSpan.Should().Be(TimeSpan.FromDays(7));
        options.SlidingExpiration.Should().BeTrue();
    }

    [Fact]
    public async Task DataProtection_Should_StoreItsKeysInTheDatabase()
    {
        // Arrange
        var protector = _scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("auth-cookie-test");

        // Act: protecting a payload creates the key ring when there is none yet.
        var payload = protector.Protect("signed-in");

        // Assert
        protector.Unprotect(payload).Should().Be("signed-in");
        (await Context.DataProtectionKeys.AnyAsync(TestContext.Current.CancellationToken)).Should().BeTrue();
    }
}

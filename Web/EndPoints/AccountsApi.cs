using Auth0.AspNetCore.Authentication;
using Carter;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Web.EndPoints;

public sealed class AccountsApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/Account/Login", async (HttpContext httpContext, string redirectUri = "/") =>
        {
            var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
                    .WithRedirectUri(redirectUri)
                    .Build();

            // To Allow SSL offloading : Github Issue #522
            httpContext.Request.IsHttps = true;
            await httpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
        });

        app.MapGet("/Account/Logout", async (HttpContext httpContext, string redirectUri = "/") =>
        {
            var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
                    .WithRedirectUri(redirectUri)
                    .Build();

            // To Allow SSL offloading : Github Issue #522
            httpContext.Request.IsHttps = true;
            await httpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        });
    }
}

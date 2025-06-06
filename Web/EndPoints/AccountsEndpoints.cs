﻿using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Web.EndPoints;

// Source : https://github.com/auth0-blog/blazor-interactive-auto/blob/main/BlazorIntAuto/Program.cs

internal static class AccountsEndpoints
{
    internal static void RegisterAccountEndpoints(this WebApplication app)
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

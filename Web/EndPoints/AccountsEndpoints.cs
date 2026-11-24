using System.Diagnostics.CodeAnalysis;
using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Web.EndPoints;

// Source : https://github.com/auth0-blog/blazor-interactive-auto/blob/main/BlazorIntAuto/Program.cs

internal static class AccountsEndpoints
{
    internal static void RegisterAccountEndpoints(this WebApplication app)
    {
        app.MapGet("/Account/Login", LoginAsync);
        app.MapGet("/Account/Logout", LogoutAsync);
    }

    internal static async Task LoginAsync(HttpContext httpContext, string redirectUri = "/")
    {
        var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
                .WithRedirectUri(ToLocalPath(redirectUri))
                .Build();
        authenticationProperties.IsPersistent = true;

        // To Allow SSL offloading : Github Issue #522
        httpContext.Request.IsHttps = true;
        await httpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
    }

    internal static async Task LogoutAsync(HttpContext httpContext, string redirectUri = "/")
    {
        var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
                .WithRedirectUri(ToLocalPath(redirectUri))
                .Build();

        // To Allow SSL offloading : Github Issue #522
        httpContext.Request.IsHttps = true;
        await httpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    // Only a path of this site is followed after sign-in or sign-out, so a forged link cannot send
    // the user to another site (#1367). "//host" and "/\\host" are absolute for a browser.
    internal static string ToLocalPath(string? redirectUri) =>
        IsLocalPath(redirectUri) ? redirectUri : "/";

    private static bool IsLocalPath([NotNullWhen(true)] string? uri) =>
        !string.IsNullOrEmpty(uri)
        && uri[0] == '/'
        && (uri.Length == 1 || (uri[1] != '/' && uri[1] != '\\'))
        // Browsers drop tabs and line breaks: "/\t/host" would become "//host".
        && !uri.Any(char.IsControl);
}

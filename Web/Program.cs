using Web.Components;
using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Web.Configuration;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Components.Authorization;
using Web;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// Load Auth0 Configuration
var config = configuration.GetSection("Auth0");
builder.Services.Configure<Auth0Configuration>(config);
builder.Services.AddSingleton<IAuth0Configuration>(sp => sp.GetRequiredService<IOptions<Auth0Configuration>>().Value);
var auth0Config = config.Get<Auth0Configuration>();
Guard.Against.Null(auth0Config);

// Register CustomAuthenticationStateProvider
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Add Auth0 services
builder.Services
    .AddAuth0WebAppAuthentication(options => {
        options.Domain = auth0Config.Domain;
        options.ClientId = auth0Config.ClientId;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/Account/Login", async (HttpContext httpContext, string redirectUri = "/") =>
{
    var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
            .WithRedirectUri(redirectUri)
            .Build();

    await httpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
});

app.MapGet("/Account/Logout", async (HttpContext httpContext, string redirectUri = "/") =>
{
    var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
            .WithRedirectUri(redirectUri)
            .Build();

    await httpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
});

app.MapRazorComponents<App>();

app.Run();

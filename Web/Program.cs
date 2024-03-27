using Web.Components;
using Auth0.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Web.Configuration;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Components.Authorization;
using Serilog;
using Web;
using Application;
using Persistence;
using WebAPI;
using Carter;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using HealthChecks.ApplicationStatus.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(nameof(MongoDbOptions)));
var options = builder.Configuration.GetSection(nameof(MongoDbOptions)).Get<MongoDbOptions>();
Guard.Against.Null(options);

// Config Global Exception Management
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Config API
builder.Services.AddCarter();

builder.Services
    .AddApplication()
    .AddInfrastructure(options.ConnectionString, options.DatabaseName);

// Config Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Config Auth0
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

// Config HealthChecks
builder.Services.AddHealthChecks()
    .AddApplicationStatus()
    .AddMongoDb(options.ConnectionString);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>();

app.MapCarter();

app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();

#pragma warning disable S1118 // Utility classes should not have public constructors
public partial class Program { }
#pragma warning restore S1118 // Util

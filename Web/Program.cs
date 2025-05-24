using Application;
using Ardalis.GuardClauses;
using Auth0.AspNetCore.Authentication;
using Carter;
using HealthChecks.ApplicationStatus.DependencyInjection;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MudBlazor;
using MudBlazor.Services;
using Persistence;
using Serilog;
using Web;
using Web.Components;
using Web.Configuration;
using Web.Services;

var builder = WebApplication.CreateBuilder(args);
Guard.Against.Null(builder);

var configuration = builder.Configuration;

// Config Global Exception Management
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Config API
builder.Services.AddCarter();

// Config MongoDb
builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(nameof(MongoDbOptions)));
var optionsMongoDb = builder.Configuration.GetSection(nameof(MongoDbOptions)).Get<MongoDbOptions>();
Guard.Against.Null(optionsMongoDb);

// Config LocalStorage
var localStorageSection = builder.Configuration.GetSection("LocalStorage");
builder.Services.Configure<LocalStorageConfiguration>(localStorageSection);
var localStorageConfig = localStorageSection.Get<LocalStorageConfiguration>();
Guard.Against.Null(localStorageConfig);

builder.Services
    .AddApplication()
    .AddInfrastructure(optionsMongoDb.ConnectionString, optionsMongoDb.DatabaseName, localStorageConfig.RootPath);

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
    .AddAuth0WebAppAuthentication(options =>
    {
        options.Domain = auth0Config.Domain;
        options.ClientId = auth0Config.ClientId;
    });

builder.Services.AddAuthorization();

// Config HealthChecks
builder.Services
    .AddSingleton(sp => new MongoClient(optionsMongoDb.ConnectionString))
    .AddHealthChecks()
    .AddApplicationStatus()
    .AddMongoDb();

// Config MudBlazor Services
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

// Config Services
builder.Services.AddScoped<ILibrariesService, LibrariesService>();

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

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapCarter();

app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

StartupInfo.Print();

app.Run();

#pragma warning disable S1118, CA1515 // Utility classes should not have public constructors
public partial class Program { }
#pragma warning restore S1118, CA1515 // Util

using Application;
using Application.ComicInfoSearch;
using Ardalis.GuardClauses;
using Auth0.AspNetCore.Authentication;
using HealthChecks.ApplicationStatus.DependencyInjection;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MudBlazor;
using MudBlazor.Services;
using Persistence;
using Serilog;
using Web;
using Web.Components;
using Web.Configuration;
using Web.EndPoints;
using Web.Services;

var builder = WebApplication.CreateBuilder(args);
Guard.Against.Null(builder);

var configuration = builder.Configuration;

// Config Global Exception Management
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Get connection string from configuration
var connectionString = configuration.GetConnectionString("NeonConnection");
Guard.Against.NullOrWhiteSpace(connectionString);

// Config LocalStorage
var localStorageSection = builder.Configuration.GetSection("LocalStorage");
builder.Services.AddOptions<LocalStorageConfiguration>()
    .Bind(localStorageSection)
    .Validate(cfg => !string.IsNullOrWhiteSpace(cfg.RootPath), "LocalStorage:RootPath is required")
    .Validate(cfg => Path.IsPathFullyQualified(cfg.RootPath), "LocalStorage:RootPath must be an absolute path")
    .ValidateOnStart();

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString, configuration["LocalStorage:RootPath"]!);

// Config Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Config Auth0
var config = configuration.GetSection("Auth0");
builder.Services.AddOptions<Auth0Configuration>()
    .Bind(config)
    .Validate(cfg => !string.IsNullOrWhiteSpace(cfg.ClientId), "Auth0:ClientId is required")
    .Validate(cfg => !string.IsNullOrWhiteSpace(cfg.Domain), "Auth0:Domain is required")
    .ValidateOnStart();

// Add Auth0 services
builder.Services.AddAuth0WebAppAuthentication(options => config.Bind(options));

// Register CustomAuthenticationStateProvider
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Config HealthChecks
builder.Services
    .AddHealthChecks()
    .AddApplicationStatus()
    .AddNpgSql(connectionString);

// Config MudBlazor Services
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
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
builder.Services.AddScoped<IBooksService, BooksService>();

// Config OpenLibrary service for ISBN lookup
builder.Services.AddHttpClient<IOpenLibraryService, OpenLibraryService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "MyComicsManager/1.0 (https://github.com/slucky31/mycomicsmanager)");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Config Cloudinary service for cover image storage
var cloudinarySection = configuration.GetSection("Cloudinary");
builder.Services.AddOptions<CloudinarySettings>()
    .Bind(cloudinarySection)
    .ValidateOnStart();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

builder.Services.AddScoped<IComicSearchService, ComicSearchService>();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// Register Accounts Endpoints for Auth0 login/logout
app.RegisterAccountEndpoints();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

StartupInfo.Print();

app.Run();

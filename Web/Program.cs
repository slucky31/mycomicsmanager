using Application;
using Application.ComicInfoSearch;
using Application.ImportJobs;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Auth0.AspNetCore.Authentication;
using Hangfire;
using Hangfire.PostgreSql;
using HealthChecks.ApplicationStatus.DependencyInjection;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;
using Persistence;
using Serilog;
using Web;
using Web.Components;
using Web.Configuration;
using Web.EndPoints;
using Web.Infrastructure;
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

// Config Import settings
var importSection = builder.Configuration.GetSection("Import");
builder.Services.AddOptions<ImportSettings>()
    .Bind(importSection)
    .Validate(cfg => !string.IsNullOrWhiteSpace(cfg.ImportDirectory), "Import:ImportDirectory is required")
    .Validate(cfg => !string.IsNullOrWhiteSpace(cfg.TempDirectory), "Import:TempDirectory is required")
    .ValidateOnStart();

// Config Hangfire with PostgreSQL
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connectionString)));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1; // Sequential for RPi4
    options.Queues = ["import", "default"];
});

// Config LocalStorage
var localStorageSection = builder.Configuration.GetSection("LocalStorage");
builder.Services.AddOptions<LocalStorageConfiguration>()
    .Bind(localStorageSection)
    .Validate(cfg => !string.IsNullOrWhiteSpace(cfg.RootPath), "LocalStorage:RootPath is required")
    .Validate(cfg => Path.IsPathFullyQualified(cfg.RootPath), "LocalStorage:RootPath must be an absolute path")
    .ValidateOnStart();

// Config Cloudinary settings
var cloudinarySection = configuration.GetSection("Cloudinary");
builder.Services.AddOptions<CloudinarySettings>()
    .Bind(cloudinarySection)
    .ValidateOnStart();

// Config OpenLibrary service for ISBN lookup
builder.Services.AddHttpClient<IOpenLibraryService, OpenLibraryService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "MyComicsManager/1.0 (https://github.com/slucky31/mycomicsmanager)");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Config Google Books settings
var googleBooksSection = configuration.GetSection("GoogleBooks");
builder.Services.AddOptions<GoogleBooksSettings>()
    .Bind(googleBooksSection)
    .Validate(cfg => cfg.BaseUrl is not null, "GoogleBooks:BaseUrl is required")
    .ValidateOnStart();

// Config Google Books service for ISBN lookup (fallback)
builder.Services.AddHttpClient<IGoogleBooksService, GoogleBooksService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "MyComicsManager/1.0 (https://github.com/slucky31/mycomicsmanager)");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Config Bedetheque settings
var bedethequeSection = configuration.GetSection("Bedetheque");
builder.Services.AddOptions<BedethequeSettings>()
    .Bind(bedethequeSection)
    .ValidateOnStart();

// Config Bedetheque HTTP clients
builder.Services.AddHttpClient("Bedetheque", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient("SerpApi", client => client.Timeout = TimeSpan.FromSeconds(15));

// Config Bedetheque service
builder.Services.AddScoped<IBedethequeService, BedethequeService>();

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString, configuration["LocalStorage:RootPath"]!, configuration);

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
builder.Services.AddAuth0WebAppAuthentication(options =>
{
    config.Bind(options);
    options.Scope = "openid profile email";
});

builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromDays(3);
        options.SlidingExpiration = true;
    });

// Register CustomAuthenticationStateProvider
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Config HealthChecks
builder.Services
    .AddHealthChecks()
    .AddApplicationStatus()
    .AddNpgSql(connectionString)
    .AddCheck<ImportDirectoryHealthCheck>("import-directory");

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
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<LibraryStateService>();
builder.Services.AddSingleton<IImportJobEnqueuer, HangfireImportJobEnqueuer>();
builder.Services.AddHostedService<FileWatcherService>();
builder.Services.AddHostedService<IconPickerWarmupService>();

var app = builder.Build();

// Ensure import and temp directories exist at startup
var importSettings = app.Services.GetRequiredService<IOptions<ImportSettings>>().Value;
Directory.CreateDirectory(importSettings.ImportDirectory);
Directory.CreateDirectory(importSettings.TempDirectory);
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

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAuthorizationFilter()]
});

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// Register Accounts Endpoints for Auth0 login/logout
app.RegisterAccountEndpoints();

// Register Books download endpoint
app.RegisterBooksEndpoints();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

StartupInfo.Print();

app.Run();

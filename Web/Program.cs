using Application;
using Application.ComicInfoSearch;
using Application.ImportJobs;
using Application.ImportJobs.Process;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
var connectionString = configuration.GetConnectionString("DefaultConnection");
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
    .Validate(cfg => Directory.Exists(cfg.RootPath), "LocalStorage:RootPath does not exist; check the volume is mounted")
    .ValidateOnStart();

// Config OpenLibrary settings
var openLibrarySection = configuration.GetSection("OpenLibrary");
builder.Services.AddOptions<OpenLibrarySettings>()
    .Bind(openLibrarySection)
    .ValidateOnStart();

// Config OpenLibrary service for ISBN lookup
builder.Services.AddHttpClient<IOpenLibraryService, OpenLibraryService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "MyComicsManager/1.0 (https://github.com/slucky31/mycomicsmanager)");
    client.Timeout = TimeSpan.FromSeconds(30);
})
    .AddHttpMessageHandler(sp => new SsrfGuardHandler(
        sp.GetRequiredService<ILogger<SsrfGuardHandler>>(),
        new HashSet<string>(["openlibrary.org", "covers.openlibrary.org"], StringComparer.OrdinalIgnoreCase)))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });

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
})
    .AddHttpMessageHandler(sp => new SsrfGuardHandler(
        sp.GetRequiredService<ILogger<SsrfGuardHandler>>(),
        new HashSet<string>(["www.googleapis.com", "books.googleapis.com"], StringComparer.OrdinalIgnoreCase)))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });

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
})
    .AddHttpMessageHandler(sp => new SsrfGuardHandler(
        sp.GetRequiredService<ILogger<SsrfGuardHandler>>(),
        new HashSet<string>(["www.bedetheque.com"], StringComparer.OrdinalIgnoreCase)))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
builder.Services.AddHttpClient("SerpApi", client => client.Timeout = TimeSpan.FromSeconds(15))
    .AddHttpMessageHandler(sp => new SsrfGuardHandler(
        sp.GetRequiredService<ILogger<SsrfGuardHandler>>(),
        new HashSet<string>(["serpapi.com"], StringComparer.OrdinalIgnoreCase)))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });

// Config Bedetheque service
builder.Services.AddScoped<IBedethequeService, BedethequeService>();

// Config feed import (Miniflux starred entries -> FeedImportDecisions)
builder.Services.AddFeedImport(configuration);

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString, configuration["LocalStorage:RootPath"]!, configuration);

// Config Serilog: levels from Serilog:MinimumLevel, tunable at runtime by admins (LogLevelSwitches),
// HTTP traffic and business logs in two separate files
var logLevelSwitches = LogLevelSwitches.FromConfiguration(configuration,
    [typeof(Program).Assembly, typeof(ApplicationDependencyInjection).Assembly, typeof(ProjectDependencyInjection).Assembly]);
builder.Services.AddSingleton(logLevelSwitches);
builder.Host.UseSerilog((context, loggerConfiguration) =>
    logLevelSwitches.ApplyTo(loggerConfiguration.ReadFrom.Configuration(context.Configuration))
        .WriteToSplitFiles());

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
// One singleton cache per check, shared across calls, so third-party APIs (Cloudinary,
// Debrid-Link, Miniflux) aren't re-pinged on every hit; the checks themselves stay at
// AddCheck<T>'s default lifetime since they depend on scoped/transient services.
builder.Services.AddSingleton(typeof(HealthCheckResultCache<>));
builder.Services
    .AddHealthChecks()
    .AddApplicationStatus()
    .AddNpgSql(connectionString)
    .AddCheck<ImportDirectoryHealthCheck>("import-directory")
    .AddCheck<CloudinaryHealthCheck>("cloudinary")
    .AddCheck<DebridLinkHealthCheck>("debrid-link")
    .AddCheck<MinifluxHealthCheck>("miniflux");

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
builder.Services.AddScoped<IBookMoveService, BookMoveService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<ImportJobHandlers>();
builder.Services.AddScoped<ProcessImportJobRepositories>();
builder.Services.AddScoped<ProcessImportJobFileProcessors>();
builder.Services.AddScoped<ProcessImportJobExternalServices>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<LibraryStateService>();
builder.Services.AddScoped<IImportOrchestrator, ImportOrchestrator>();
builder.Services.AddSingleton<IImportJobEnqueuer, HangfireImportJobEnqueuer>();
builder.Services.AddHostedService<FileWatcherService>();
builder.Services.AddHostedService<IconPickerWarmupService>();

var app = builder.Build();

// Apply pending EF Core migrations automatically on startup
// ponytail: no distributed lock, fine for a single instance; add one (e.g. pg_advisory_lock) if this ever runs with multiple replicas
using (var migrationScope = app.Services.CreateScope())
{
    await migrationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
}

// Ensure import and temp directories exist at startup
var importSettings = app.Services.GetRequiredService<IOptions<ImportSettings>>().Value;
Directory.CreateDirectory(importSettings.ImportDirectory);
Directory.CreateDirectory(importSettings.TempDirectory);

// Register (or remove) the recurring Miniflux sync according to FeedImport:Enabled
app.Services.ScheduleFeedImportSync();

// Refuse to start if a dependency (database, Cloudinary, import directory, ...) is unreachable,
// rather than accepting traffic and failing later on the first request that needs it.
// Degraded checks (optional feed import services: Debrid-Link, Miniflux) only log a warning.
using (var healthScope = app.Services.CreateScope())
{
    var healthCheckService = healthScope.ServiceProvider.GetRequiredService<HealthCheckService>();
    var startupHealthReport = await healthCheckService.CheckHealthAsync();
    foreach (var entry in startupHealthReport.Entries.Where(e => e.Value.Status == HealthStatus.Degraded))
    {
        Log.Warning("Startup health check degraded: {Check} - {Description}", entry.Key, entry.Value.Description);
    }
    if (startupHealthReport.Status == HealthStatus.Unhealthy)
    {
        var unhealthyEntries = startupHealthReport.Entries.Where(e => e.Value.Status == HealthStatus.Unhealthy).ToList();
        foreach (var entry in unhealthyEntries)
        {
            Log.Fatal("Startup health check failed: {Check} - {Description}", entry.Key, entry.Value.Description);
        }
        var summary = string.Join("; ", unhealthyEntries.Select(e => $"{e.Key}: {e.Value.Description}"));
        throw new InvalidOperationException($"One or more startup health checks failed: {summary}");
    }
}

app.UseSerilogRequestLogging(options => options.GetLevel = (httpContext, _, exception) => LoggingConfiguration.GetRequestLevel(httpContext, exception));

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    await next();
});

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

StartupInfo.Print(app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(StartupInfo)));

await app.RunAsync();

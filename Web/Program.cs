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
using Carter;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using HealthChecks.ApplicationStatus.DependencyInjection;
using MudBlazor.Services;
using MudBlazor;
using Web.Services;
using Sentry.Profiling;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);
Guard.Against.Null(builder);

var configuration = builder.Configuration;

// Config Sentry
var config = configuration.GetSection("Sentry");
builder.Services.Configure<SentryOption>(config);
builder.Services.AddSingleton<ISentryOption>(sp => sp.GetRequiredService<IOptions<SentryOption>>().Value);
var sentryConfig = config.Get<SentryOption>();
Guard.Against.Null(sentryConfig);

// Configire OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithMetrics(builder => builder
        .AddAspNetCoreInstrumentation()        
    )
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation() // <-- Adds ASP.NET Core telemetry sources
            .AddHttpClientInstrumentation() // <-- Adds HttpClient telemetry sources
            .AddSentry() // <-- Configure OpenTelemetry to send trace information to Sentry
);


builder.WebHost.UseSentry(o =>
{
    o.Dsn = sentryConfig.Dsn;
    // Set TracesSampleRate to 1.0 to capture 100%
    // of transactions for performance monitoring.
    // We recommend adjusting this value in production
    o.TracesSampleRate = 1.0;
    // Sample rate for profiling, applied on top of othe TracesSampleRate,
    // e.g. 0.2 means we want to profile 20 % of the captured transactions.
    // We recommend adjusting this value in production.
    o.ProfilesSampleRate = 1.0;
    // Requires NuGet package: Sentry.Profiling
    // Note: By default, the profiler is initialized asynchronously. This can
    // be tuned by passing a desired initialization timeout to the constructor.
    o.AddIntegration(new ProfilingIntegration(
        // During startup, wait up to 500ms to profile the app startup code.
        // This could make launching the app a bit slower so comment it out if you
        // prefer profiling to start asynchronously.
        TimeSpan.FromMilliseconds(500)
    ));
    o.Environment = sentryConfig.Environment;
    // Configure Sentry to use OpenTelemetry trace information
    o.UseOpenTelemetry();
});

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
config = configuration.GetSection("Auth0");
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

// Config Web Services
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

SentrySdk.CaptureMessage("Hello Sentry : MCM Run()");
app.Run();

#pragma warning disable S1118 // Utility classes should not have public constructors
public partial class Program { }
#pragma warning restore S1118 // Util

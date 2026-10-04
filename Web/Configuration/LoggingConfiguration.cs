using System.Globalization;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace Web.Configuration;

internal static class LoggingConfiguration
{
    // Plain text, read by humans (tail/grep on the Pi): "2026-10-04 18:00:14.118 +02:00 [INF] Web.Services.FeedImportSyncJob: ...".
    private const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    // HTTP traffic: outgoing HttpClient calls (4 lines each) and incoming requests
    // (UseSerilogRequestLogging, ASP.NET Core hosting diagnostics).
    private static readonly Func<LogEvent, bool>[] s_httpSources =
    [
        Matching.FromSource("System.Net.Http.HttpClient"),
        Matching.FromSource("Serilog.AspNetCore.RequestLoggingMiddleware"),
        Matching.FromSource("Microsoft.AspNetCore.Hosting.Diagnostics"),
    ];

    internal static bool IsHttp(LogEvent logEvent) => s_httpSources.Any(matches => matches(logEvent));

    // Serilog's default levels, except the Docker health check (every 30 s) which only shows up when it fails.
    internal static LogEventLevel GetRequestLevel(HttpContext httpContext, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }

        return httpContext.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ? LogEventLevel.Verbose : LogEventLevel.Information;
    }

    // Two rolling files so the HTTP noise doesn't drown the business logs: http-<date>.txt and log-<date>.txt.
    // The sub-loggers accept every level: the levels are filtered upstream (Serilog:MinimumLevel and LogLevelSwitches).
    internal static LoggerConfiguration WriteToSplitFiles(this LoggerConfiguration configuration, string directory = "log")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration
            .WriteTo.Logger(http => http
                .MinimumLevel.Verbose()
                .Filter.ByIncludingOnly(IsHttp)
                .WriteTo.File(Path.Combine(directory, "http-.txt"), outputTemplate: OutputTemplate, formatProvider: CultureInfo.InvariantCulture, rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true))
            .WriteTo.Logger(business => business
                .MinimumLevel.Verbose()
                .Filter.ByExcluding(IsHttp)
                .WriteTo.File(Path.Combine(directory, "log-.txt"), outputTemplate: OutputTemplate, formatProvider: CultureInfo.InvariantCulture, rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true));
    }
}

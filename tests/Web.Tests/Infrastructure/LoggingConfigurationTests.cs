using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Events;
using Web.Configuration;
using Xunit;

namespace Web.Tests.Infrastructure;

public sealed class LoggingConfigurationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"mcm-logs-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private string ReadFile(string prefix) =>
        File.ReadAllText(Directory.GetFiles(_directory, $"{prefix}-*.txt").Single());

    [Theory]
    [InlineData("/health", 200, LogEventLevel.Verbose)]
    [InlineData("/health", 503, LogEventLevel.Error)]
    [InlineData("/books", 200, LogEventLevel.Information)]
    [InlineData("/books", 500, LogEventLevel.Error)]
    public void GetRequestLevel_Should_HideHealthChecks_WhenTheySucceed(string path, int statusCode, LogEventLevel expected)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        httpContext.Response.StatusCode = statusCode;

        LoggingConfiguration.GetRequestLevel(httpContext, null).Should().Be(expected);
    }

    [Fact]
    public void WriteToSplitFiles_Should_WriteHttpLogsAndBusinessLogsToSeparateFiles()
    {
        using (var logger = new LoggerConfiguration().MinimumLevel.Debug().WriteToSplitFiles(_directory).CreateLogger())
        {
            logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, "System.Net.Http.HttpClient.Miniflux.LogicalHandler")
                .Information("Start processing HTTP request");
            logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, "Serilog.AspNetCore.RequestLoggingMiddleware")
                .Information("HTTP GET /health responded 200");
            logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, "Web.Services.FeedImportSyncJob")
                .Debug("Feed import sync done");
        }

        var http = ReadFile("http");
        http.Should().Contain("Start processing HTTP request").And.Contain("responded");
        http.Should().NotContain("Feed import sync done");

        var business = ReadFile("log");
        business.Should().Contain("Feed import sync done");
        business.Should().NotContain("HttpClient").And.NotContain("responded");
    }
}

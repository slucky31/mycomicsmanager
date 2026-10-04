using System.Reflection;
using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Serilog.Events;
using Web.Components.Layout;
using Web.Components.Pages.Admin;
using Web.Infrastructure;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages;

public sealed class LogLevelsComponentTests
{
    private const string LoggerName = "Web.Services.FeedImportSyncJob";

    private static LogLevelSwitches CreateSwitches() =>
        new(LogEventLevel.Information, new Dictionary<string, LogEventLevel>(), [LoggerName]);

    private static BunitContext CreateContext(LogLevelSwitches switches)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(switches);
        ctx.Services.AddSingleton(Substitute.For<ISnackbar>());
        return ctx;
    }

    private static IElement LevelButton(IRenderedComponent<LogLevels> cut, string loggerName, string label) =>
        cut.FindAll("tr")
            .Single(row => row.QuerySelector(".logger-name")?.TextContent == loggerName)
            .QuerySelectorAll("button")
            .Single(button => button.TextContent.Trim() == label);

    [Fact]
    public void LogLevels_Should_BeRestrictedToAdmins()
    {
        typeof(LogLevels).GetCustomAttributes<AuthorizeAttribute>()
            .Should().Contain(attribute => attribute.Roles == "Admin");
    }

    [Fact]
    public async Task SetLevel_Should_ChangeTheLevelOfTheLogger_WhenALevelButtonIsClicked()
    {
        var switches = CreateSwitches();
        await using var ctx = CreateContext(switches);
        var cut = ctx.Render<LogLevels>();
        cut.Markup.Should().Contain(LoggerName).And.Contain(LogLevelSwitches.RootLoggerName);

        await LevelButton(cut, LoggerName, "Debug").ClickAsync(new());

        switches.GetLoggers().Single(l => l.Name == LoggerName).ConfiguredLevel.Should().Be(LogEventLevel.Debug);
        LevelButton(cut, LoggerName, "Debug").GetAttribute("aria-pressed").Should().Be("true");
    }

    [Fact]
    public async Task ResetAll_Should_RestoreTheConfiguredLevels_WhenResetIsClicked()
    {
        var switches = CreateSwitches();
        switches.Set(LoggerName, LogEventLevel.Error);
        await using var ctx = CreateContext(switches);
        var cut = ctx.Render<LogLevels>();

        await cut.FindAll("button").Single(b => b.TextContent.Trim() == "Reset").ClickAsync(new());

        switches.GetLoggers().Single(l => l.Name == LoggerName).ConfiguredLevel.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NavBar_Should_ShowTheLogsLink_OnlyToAdmins(bool isAdmin)
    {
        await using var ctx = CreateContext(CreateSwitches());
        ctx.Services.AddSingleton(Substitute.For<IFeedImportService>());
        ctx.Services.AddSingleton(new FeedImportNotifier());
        var authorization = ctx.AddAuthorization();
        authorization.SetAuthorized("user");
        if (isAdmin)
        {
            authorization.SetRoles("Admin");
        }

        var cut = ctx.Render<NavBar>();

        cut.FindAll("a[href='/admin/log-levels']").Count.Should().Be(isAdmin ? 2 : 0);
    }
}

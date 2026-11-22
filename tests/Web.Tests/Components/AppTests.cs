using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Web.Components;
using Web.Infrastructure;
using Xunit;

namespace Web.Tests.Components;

public sealed class AppTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();

    public AppTests()
    {
        _ctx.ComponentFactories.AddStub<Routes>();
        _ctx.ComponentFactories.AddStub<HeadOutlet>();
    }

    public async ValueTask DisposeAsync() => await _ctx.DisposeAsync();

    [Fact]
    public void App_Should_LoadTheNotificationStylesAfterTheTheme_WhenRendered()
    {
        var cut = _ctx.Render<App>();

        var stylesheets = cut.FindAll("link[rel=stylesheet]").Select(link => link.GetAttribute("href")).ToList();
        var theme = stylesheets.IndexOf(AppVersion.Versioned("css/theme.css"));
        var snackbar = stylesheets.IndexOf(AppVersion.Versioned("css/snackbar.css"));

        theme.Should().BeGreaterThanOrEqualTo(0);
        snackbar.Should().BeGreaterThan(theme);
    }
}

using AwesomeAssertions;
using Bunit;
using MudBlazor.Services;
using Web.Components.Pages.Libraries;
using Xunit;

namespace Web.Tests.Components.Pages.Libraries;

public sealed class IconChooserTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();

    public IconChooserTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
    }

    public async ValueTask DisposeAsync() => await _ctx.DisposeAsync();

    [Fact]
    public async Task SearchAsync_Should_ShowTheIconsFound_AndChooseOne()
    {
        var chosen = string.Empty;
        var cut = _ctx.Render<IconChooser>(p => p.Add(c => c.Value, "Star").Add(c => c.ValueChanged, (string icon) => chosen = icon));

        await cut.Find("input[type=search]").InputAsync(new() { Value = "auto stories" });

        cut.Find(".icon-chooser-caption").TextContent.Should().Be("1 icon");
        await cut.Find("button[aria-label='Auto stories']").ClickAsync(new());
        chosen.Should().Be("AutoStories");
    }

    [Fact]
    public async Task SearchAsync_Should_SayNothingMatches_AndShowTheSuggestionsAgain_OnceCleared()
    {
        var cut = _ctx.Render<IconChooser>(p => p.Add(c => c.Value, "Star"));

        await cut.Find("input[type=search]").InputAsync(new() { Value = "zzzz" });

        cut.Find(".icon-chooser-empty").TextContent.Should().Be("No icon matches your search");
        cut.FindAll(".icon-grid").Should().BeEmpty();

        await cut.Find("input[type=search]").InputAsync(new() { Value = "" });

        cut.Find(".icon-chooser-caption").TextContent.Should().Be("Suggested");
        cut.Find("button[aria-pressed=true]").GetAttribute("aria-label").Should().Be("Star");
    }
}

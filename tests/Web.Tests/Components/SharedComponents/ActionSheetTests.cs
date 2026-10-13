using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Services;
using Web.Components.SharedComponents;
using Web.Models;
using Xunit;

namespace Web.Tests.Components.SharedComponents;

public sealed class ActionSheetTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private int _closed;

    public ActionSheetTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
    }

    public ValueTask DisposeAsync() => _ctx.DisposeAsync();

    private IRenderedComponent<ActionSheet> RenderSheet(bool open = true, PopupAnchor? anchor = null, string? title = null) =>
        _ctx.Render<ActionSheet>(p => p
            .Add(c => c.Open, open)
            .Add(c => c.AriaLabel, "Sort and view")
            .Add(c => c.Title, title)
            .Add(c => c.Anchor, anchor)
            .Add(c => c.AlignEnd, true)
            .Add(c => c.OnClose, () => _closed++)
            .AddChildContent("<p>Options</p>"));

    [Fact]
    public void ActionSheet_Should_RenderNothing_WhenClosed() =>
        RenderSheet(open: false).Markup.Trim().Should().BeEmpty();

    [Fact]
    public void ActionSheet_Should_BeCentered_WhenItHasNoAnchor()
    {
        var panel = RenderSheet().Find("[role=dialog]");

        panel.ClassList.Should().Contain("action-sheet-centered");
        panel.GetAttribute("aria-label").Should().Be("Sort and view");
    }

    [Fact]
    public void ActionSheet_Should_OpenNextToItsAnchor()
    {
        var anchor = new PopupAnchor(700, 14, 744, 58, 1024, 768);

        var panel = RenderSheet(anchor: anchor).Find("[role=dialog]");

        panel.ClassList.Should().NotContain("action-sheet-centered");
        panel.GetAttribute("style").Should().Be(PopupPlacement.Style(anchor, alignEnd: true));
    }

    [Fact]
    public void ActionSheet_Should_FocusItsPanel_WhenItOpens()
    {
        RenderSheet();

        _ctx.JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public async Task ActionSheet_Should_Close_WhenTheBackdropIsClicked()
    {
        await RenderSheet().Find(".action-sheet-backdrop").ClickAsync(new());

        _closed.Should().Be(1);
    }

    [Fact]
    public async Task ActionSheet_Should_Close_WhenEscapeIsPressed_AndOnlyThen()
    {
        var panel = RenderSheet().Find("[role=dialog]");

        await panel.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
        await panel.KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        _closed.Should().Be(1);
    }

    [Fact]
    public async Task ActionSheet_Should_ShowItsTitleWithACloseButton_WhenItHasOne()
    {
        var cut = RenderSheet(title: "Sort and view");

        cut.Find(".action-sheet-title").TextContent.Should().Be("Sort and view");
        await cut.Find("button[aria-label='Close']").ClickAsync(new());
        _closed.Should().Be(1);
    }

    [Fact]
    public void ActionSheetItem_Should_BeALink_WhenItHasAnHref()
    {
        var item = _ctx.Render<ActionSheetItem>(p => p
            .Add(c => c.Icon, Icons.Material.Filled.Edit)
            .Add(c => c.Text, "Edit")
            .Add(c => c.Href, "/books/1/edit"));

        item.Find("a").GetAttribute("href").Should().Be("/books/1/edit");
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    [InlineData(null, null)]
    public void ActionSheetItem_Should_TellWhetherTheOptionIsSelected(bool? selected, string? expected)
    {
        var item = _ctx.Render<ActionSheetItem>(p => p
            .Add(c => c.Icon, Icons.Material.Filled.GridView)
            .Add(c => c.Text, "Covers")
            .Add(c => c.Selected, selected));

        var button = item.Find("button");
        button.GetAttribute("aria-pressed").Should().Be(expected);
        button.ClassList.Contains("action-sheet-item-selected").Should().Be(selected == true);
    }

    [Fact]
    public async Task ActionSheetItem_Should_BeRedAndCallItsAction_WhenItIsDelete()
    {
        var clicked = false;
        var item = _ctx.Render<ActionSheetItem>(p => p
            .Add(c => c.Icon, Icons.Material.Filled.Delete)
            .Add(c => c.Text, "Delete")
            .Add(c => c.Danger, true)
            .Add(c => c.OnClick, EventCallback.Factory.Create(this, () => clicked = true)));

        var button = item.Find("button");
        button.ClassList.Should().Contain("action-sheet-item-danger");
        await button.ClickAsync(new());
        clicked.Should().BeTrue();
    }
}

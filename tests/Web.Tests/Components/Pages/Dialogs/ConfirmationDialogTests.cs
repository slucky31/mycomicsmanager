using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Web.Extensions;
using Xunit;

namespace Web.Tests.Components.Pages.Dialogs;

public sealed class ConfirmationDialogTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly IRenderedComponent<MudDialogProvider> _provider;

    public ConfirmationDialogTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _provider = _ctx.Render<MudDialogProvider>();
    }

    public async ValueTask DisposeAsync()
    {
        _provider.Dispose();
        await _ctx.DisposeAsync();
    }

    private Task<bool> AskAsync(Color color = Color.Error)
    {
        var dialogs = _ctx.Services.GetRequiredService<IDialogService>();
        Task<bool>? confirmation = null;
        _provider.InvokeAsync(() => confirmation = dialogs.ShowConfirmationAsync("Delete this reading?", "The reading of 12 March 2026 will be removed.", "Delete reading", color));
        return confirmation!;
    }

    [Fact]
    public async Task ShowConfirmationAsync_Should_AskTheQuestion_AndConfirm()
    {
        var confirmation = AskAsync();

        var dialog = _provider.Find("[role=alertdialog]");
        _provider.Find($"#{dialog.GetAttribute("aria-labelledby")}").TextContent.Should().Be("Delete this reading?");
        _provider.Find($"#{dialog.GetAttribute("aria-describedby")}").TextContent.Should().Be("The reading of 12 March 2026 will be removed.");
        _provider.Find(".confirm-icon").Should().NotBeNull();
        _provider.FindAll(".confirm-button").Select(b => b.TextContent).Should().Equal("Cancel", "Delete reading");

        await _provider.Find(".confirm-danger").ClickAsync(new());

        (await confirmation).Should().BeTrue();
    }

    [Fact]
    public async Task ShowConfirmationAsync_Should_NotConfirm_WhenCancelled()
    {
        var confirmation = AskAsync();

        await _provider.Find(".confirm-cancel").ClickAsync(new());

        (await confirmation).Should().BeFalse();
    }

    [Fact]
    public async Task ShowConfirmationAsync_Should_UseThePrimaryColor_WhenTheActionDestroysNothing()
    {
        var confirmation = AskAsync(Color.Primary);

        _provider.FindAll(".confirm-icon").Should().BeEmpty();
        _provider.FindAll(".confirm-danger").Should().BeEmpty();
        await _provider.Find(".confirm-action").ClickAsync(new());

        (await confirmation).Should().BeTrue();
    }
}

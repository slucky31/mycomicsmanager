using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Web.Components.Pages.Dialogs;
using Web.Models;
using Xunit;

namespace Web.Tests.Components.Pages.Dialogs;

public sealed class MoveBookDialogTests
{
    private static async Task<(BunitContext Ctx, IRenderedComponent<MudDialogProvider> Provider, IDialogReference Dialog)> ShowAsync(
        IReadOnlyList<BookMoveTargetViewModel> targets)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Render<MudPopoverProvider>();
        var provider = ctx.Render<MudDialogProvider>();
        var dialogs = ctx.Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<MoveBookDialog> { { x => x.Targets, targets } };
        IDialogReference? dialog = null;
        await provider.InvokeAsync(async () => dialog = await dialogs.ShowAsync<MoveBookDialog>("Move", parameters));
        return (ctx, provider, dialog!);
    }

    [Fact]
    public async Task Confirm_Should_ReturnSuggestedLibrary_WhenUserKeepsThePreselection()
    {
        var suggested = Guid.CreateVersion7();
        var (ctx, provider, dialog) = await ShowAsync(
        [
            new BookMoveTargetViewModel(Guid.CreateVersion7(), "Mangas", "#111111", null, false),
            new BookMoveTargetViewModel(suggested, "BD", "#222222", "2 books of this series", true)
        ]);
        await using var _ = ctx;

        await provider.Find("button.mud-button-filled").ClickAsync(new());

        var result = await dialog.Result;
        result!.Canceled.Should().BeFalse();
        result.Data.Should().Be(suggested);
    }

    [Fact]
    public async Task Render_Should_DisableMove_WhenNothingIsSuggested_AndCancelShouldCloseWithoutResult()
    {
        var (ctx, provider, dialog) = await ShowAsync([new BookMoveTargetViewModel(Guid.CreateVersion7(), "Mangas", "#111111", null, false)]);
        await using var _ = ctx;

        provider.Find("button.mud-button-filled").HasAttribute("disabled").Should().BeTrue();
        await provider.FindAll("button").First(b => b.TextContent.Contains("Cancel", StringComparison.Ordinal)).ClickAsync(new());

        (await dialog.Result)!.Canceled.Should().BeTrue();
    }
}

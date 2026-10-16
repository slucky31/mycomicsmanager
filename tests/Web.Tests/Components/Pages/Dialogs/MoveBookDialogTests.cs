using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Web.Components.Pages.Dialogs;
using Web.Extensions;
using Web.Models;
using Xunit;

namespace Web.Tests.Components.Pages.Dialogs;

public sealed class MoveBookDialogTests : IAsyncDisposable
{
    private static readonly Guid s_current = Guid.CreateVersion7();
    private static readonly Guid s_comics = Guid.CreateVersion7();
    private static readonly Guid s_mangas = Guid.CreateVersion7();

    private readonly BunitContext _ctx = new();
    private readonly IRenderedComponent<MudDialogProvider> _provider;

    public MoveBookDialogTests()
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

    private static BookMoveChoices Choices(bool suggested) => new(
        [
            new BookMoveTargetViewModel(s_current, "À trier", "#111111", "Archive", "Digital · current library", false, true),
            new BookMoveTargetViewModel(s_comics, "Comics", "#222222", "Bookmark", "Digital · 112 books", suggested),
            new BookMoveTargetViewModel(s_mangas, "Mangas", "#333333", "Star", "Digital · 38 books", false),
        ],
        "À trier");

    private async Task<IDialogReference> ShowAsync(BookMoveChoices choices)
    {
        var parameters = new DialogParameters<MoveBookDialog> { { x => x.Choices, choices } };
        IDialogReference? dialog = null;
        var dialogs = _ctx.Services.GetRequiredService<IDialogService>();
        await _provider.InvokeAsync(async () => dialog = await dialogs.ShowAsync<MoveBookDialog>("Move to another library", parameters, AppDialogs.Options));
        return dialog!;
    }

    [Fact]
    public async Task Confirm_Should_MoveToTheSuggestedLibrary_WhenItIsKept()
    {
        var dialog = await ShowAsync(Choices(suggested: true));

        _provider.Find("h2").TextContent.Should().Be("Move to another library");
        _provider.Find(".move-intro").TextContent.Should().Be("This book is in À trier. Its file is moved too.");
        _provider.Find("[role=radio][aria-checked=true] .move-library-name").TextContent.Should().Be("Comics");
        _provider.Find(".move-library-suggested").TextContent.Should().Be("Suggested");
        _provider.Find("[role=radio]:disabled .move-library-details").TextContent.Should().Be("Digital · current library");
        _provider.Find(".move-confirm").TextContent.Should().Be("Move to Comics");

        await _provider.Find(".move-confirm").ClickAsync(new());

        var result = await dialog.Result;
        result!.Canceled.Should().BeFalse();
        result.Data.Should().Be(s_comics);
    }

    [Fact]
    public async Task Choose_Should_MoveToTheLibraryChosen_ButNotToTheCurrentOne()
    {
        var dialog = await ShowAsync(Choices(suggested: false));
        _provider.Find(".move-confirm").HasAttribute("disabled").Should().BeTrue();

        await _provider.FindAll("[role=radio]")[0].ClickAsync(new());
        _provider.Find(".move-confirm").HasAttribute("disabled").Should().BeTrue();

        await _provider.FindAll("[role=radio]")[2].ClickAsync(new());
        _provider.Find("[role=radio][aria-checked=true] .move-library-name").TextContent.Should().Be("Mangas");
        await _provider.Find(".move-confirm").ClickAsync(new());

        (await dialog.Result)!.Data.Should().Be(s_mangas);
    }

    [Theory]
    [InlineData(".move-cancel")]
    [InlineData(".move-close")]
    public async Task Cancel_Should_CloseWithoutMoving(string button)
    {
        var dialog = await ShowAsync(Choices(suggested: true));

        await _provider.Find(button).ClickAsync(new());

        (await dialog.Result)!.Canceled.Should().BeTrue();
    }

    [Fact]
    public async Task Confirm_Should_DoNothing_WhileNoLibraryIsChosen()
    {
        var dialog = await ShowAsync(new BookMoveChoices([new BookMoveTargetViewModel(s_mangas, "Mangas", "#333333", "Star", "Digital · 38 books", false)], null));

        _provider.Find(".move-intro").TextContent.Should().Be("Its file is moved too.");
        await _provider.Find(".move-confirm").ClickAsync(new());

        dialog.Result.IsCompleted.Should().BeFalse();
    }
}

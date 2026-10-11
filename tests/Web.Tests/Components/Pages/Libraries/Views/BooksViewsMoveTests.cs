using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;
using Web.Components.Pages.Libraries.Views;
using Web.Models;
using Xunit;

namespace Web.Tests.Components.Pages.Libraries.Views;

public sealed class BooksViewsMoveTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly BookListItemViewModel _book = new(
        Guid.CreateVersion7(), "Blacksad", "Âme rouge", null, 4, string.Empty, "Canales", "Dargaud", null, null, 0);

    public BooksViewsMoveTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
    }

    public ValueTask DisposeAsync() => _ctx.DisposeAsync();

    [Fact]
    public async Task OnMove_Should_ReceiveTheBookId_WhenMoveIsChosenFromTheCoverMenu()
    {
        var movedId = Guid.Empty;
        var provider = _ctx.Render<MudPopoverProvider>();
        var cut = _ctx.Render<BooksCoversView>(p => p
            .Add(c => c.Books, [_book])
            .Add(c => c.OnDelete, _ => { })
            .Add(c => c.OnMove, id => movedId = id));

        await ChooseMoveFromMenuAsync(cut, provider);

        movedId.Should().Be(_book.Id);
    }

    [Fact]
    public async Task OnMove_Should_ReceiveTheBookId_WhenMoveIsChosenFromTheCardMenu()
    {
        var movedId = Guid.Empty;
        var provider = _ctx.Render<MudPopoverProvider>();
        var cut = _ctx.Render<BooksCardsView>(p => p
            .Add(c => c.Books, [_book])
            .Add(c => c.OnDelete, _ => { })
            .Add(c => c.OnMove, id => movedId = id));

        await ChooseMoveFromMenuAsync(cut, provider);

        movedId.Should().Be(_book.Id);
    }

    [Fact]
    public async Task OnMove_Should_ReceiveTheBookId_WhenTheListMoveButtonIsClicked()
    {
        var movedId = Guid.Empty;
        var cut = _ctx.Render<BooksListView>(p => p
            .Add(c => c.ServerData, (_, _) => Task.FromResult(new TableData<BookListItemViewModel> { Items = [_book], TotalItems = 1 }))
            .Add(c => c.OnDelete, _ => { })
            .Add(c => c.OnMove, id => movedId = id));

        var moveButton = cut.WaitForElement("button[aria-label='Move book to another library']");
        await moveButton.ClickAsync(new());

        movedId.Should().Be(_book.Id);
    }

    private static async Task ChooseMoveFromMenuAsync(IRenderedComponent<IComponent> cut, IRenderedComponent<MudPopoverProvider> provider)
    {
        await cut.Find("button[aria-label='More actions']").ClickAsync(new());
        var moveItem = provider.WaitForElement(".mud-menu-item:contains('Move to')");
        await moveItem.ClickAsync(new());
    }
}

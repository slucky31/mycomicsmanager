using AwesomeAssertions;
using Bunit;
using MudBlazor.Services;
using Web.Components.Pages.Libraries.Views;
using Web.Models;
using Xunit;

namespace Web.Tests.Components.Pages.Libraries.Views;

public sealed class BooksViewsTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly BookListItemViewModel _book = new(
        Guid.CreateVersion7(), "Blacksad", "Âme rouge", null, 4, "/covers/blacksad.webp", "Canales", "Dargaud", null, 4, 2);

    public BooksViewsTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
    }

    public ValueTask DisposeAsync() => _ctx.DisposeAsync();

    private IRenderedComponent<BooksCoversView> RenderCovers(Action<BookListItemViewModel>? onActions = null) =>
        _ctx.Render<BooksCoversView>(p => p
            .Add(c => c.Books, [_book])
            .Add(c => c.OnActions, b => onActions?.Invoke(b)));

    private IRenderedComponent<BooksListView> RenderList(Action<BookListItemViewModel>? onActions = null) =>
        _ctx.Render<BooksListView>(p => p
            .Add(c => c.Books, [_book])
            .Add(c => c.OnActions, b => onActions?.Invoke(b)));

    [Fact]
    public void BooksCoversView_Should_LinkTheCoverToTheBook_WithItsVolumeAndSeries()
    {
        var cut = RenderCovers();

        cut.Find("a.cover-link").GetAttribute("href").Should().Be($"/books/{_book.Id}");
        cut.Find("img").GetAttribute("alt").Should().Be("Cover of Âme rouge");
        cut.Find(".cover-volume").TextContent.Should().Be("#4");
        cut.Find(".cover-serie").TextContent.Should().Be("Blacksad");
        cut.Find(".cover-item").GetAttribute("data-book-id").Should().Be(_book.Id.ToString());
    }

    [Fact]
    public void BooksListView_Should_ShowTheSeriesAuthorsRatingAndReadState()
    {
        var cut = RenderList();

        cut.Find(".list-title").TextContent.Should().Be("Âme rouge");
        cut.FindAll(".list-meta").Select(e => e.TextContent).Should().Equal("Blacksad · #4", "Canales");
        cut.Find(".list-rating").GetAttribute("aria-label").Should().Be("Rated 4 out of 5");
        cut.Find(".list-read").TextContent.Should().Be("Read");
    }

    [Fact]
    public void BooksListView_Should_LeaveOutTheRatingAndReadState_WhenTheBookWasNeverRead()
    {
        var cut = _ctx.Render<BooksListView>(p => p
            .Add(c => c.Books, [_book with { LastRating = null, ReadCount = 0 }])
            .Add(c => c.OnActions, _ => { }));

        cut.FindAll(".list-rating").Should().BeEmpty();
        cut.FindAll(".list-read").Should().BeEmpty();
    }

    [Fact]
    public async Task OnActions_Should_ReceiveTheBook_WhenTheCoverActionsButtonIsClicked()
    {
        BookListItemViewModel? chosen = null;
        var cut = RenderCovers(b => chosen = b);

        await cut.Find($"#{BookActionsIds.Of(_book.Id)}").ClickAsync(new());

        chosen.Should().Be(_book);
    }

    [Fact]
    public async Task OnActions_Should_ReceiveTheBook_WhenTheListActionsButtonIsClicked()
    {
        BookListItemViewModel? chosen = null;
        var cut = RenderList(b => chosen = b);

        await cut.Find("button[aria-label='More actions for Âme rouge']").ClickAsync(new());

        chosen.Should().Be(_book);
    }
}

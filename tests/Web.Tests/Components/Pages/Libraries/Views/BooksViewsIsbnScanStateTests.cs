using AwesomeAssertions;
using Bunit;
using Domain.Books;
using MudBlazor;
using MudBlazor.Services;
using Web.Components.Pages.Libraries.Views;
using Web.Models;
using Xunit;

namespace Web.Tests.Components.Pages.Libraries.Views;

public sealed class BooksViewsIsbnScanStateTests : IAsyncDisposable
{
    private const string CandidatesLabel = "Several ISBNs found in the pages: pick the book's own";

    private readonly BunitContext _ctx = new();

    public BooksViewsIsbnScanStateTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
    }

    public ValueTask DisposeAsync() => _ctx.DisposeAsync();

    private static BookListItemViewModel Book(string? isbn, IsbnScanState state) =>
        new(Guid.CreateVersion7(), "Blacksad", "Âme rouge", isbn, 4, string.Empty, "Canales", "Dargaud", null, null, 0, state);

    private string Cards(BookListItemViewModel book) => _ctx.Render<BooksCardsView>(p => p.Add(c => c.Books, [book])).Markup;

    private string Covers(BookListItemViewModel book) => _ctx.Render<BooksCoversView>(p => p.Add(c => c.Books, [book])).Markup;

    private async Task<string> ListAsync(BookListItemViewModel book)
    {
        var cut = _ctx.Render<BooksListView>(p => p
            .Add(c => c.ServerData, (_, _) => Task.FromResult(new TableData<BookListItemViewModel> { Items = [book], TotalItems = 1 })));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Âme rouge"));
        return cut.Markup;
    }

    [Fact]
    public void BooksCardsView_Should_ShowTheIsbnScanState_WhenTheBookHasNoIsbn()
    {
        Cards(Book(null, IsbnScanState.Candidates)).Should().Contain(CandidatesLabel);
        Cards(Book("9782800112343", IsbnScanState.HasIsbn)).Should().NotContain("aria-label=\"No ISBN");
    }

    [Fact]
    public void BooksCoversView_Should_ShowTheIsbnScanState_WhenTheBookHasNoIsbn()
    {
        Covers(Book(null, IsbnScanState.NotFound)).Should().Contain("No ISBN found in the pages");
        Covers(Book("9782800112343", IsbnScanState.HasIsbn)).Should().NotContain("book-cover-isbn-state");
    }

    [Fact]
    public async Task BooksListView_Should_ShowTheIsbnScanState_WhenTheBookHasNoIsbn()
    {
        (await ListAsync(Book(null, IsbnScanState.NotScanned))).Should().Contain("No ISBN: the pages were not scanned yet");
        (await ListAsync(Book("9782800112343", IsbnScanState.HasIsbn))).Should().Contain("9782800112343");
    }
}

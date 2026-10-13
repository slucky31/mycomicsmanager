using AwesomeAssertions;
using Bunit;
using Domain.Books;
using MudBlazor.Services;
using Web.Components.Pages.Libraries.Views;
using Web.Models;
using Xunit;

namespace Web.Tests.Components.Pages.Libraries.Views;

public sealed class BooksViewsIsbnScanStateTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();

    public BooksViewsIsbnScanStateTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
    }

    public ValueTask DisposeAsync() => _ctx.DisposeAsync();

    private static BookListItemViewModel Book(string? isbn, IsbnScanState state) =>
        new(Guid.CreateVersion7(), "Blacksad", "Âme rouge", isbn, 4, string.Empty, "Canales", "Dargaud", null, null, 0, state, state == IsbnScanState.Candidates ? 2 : 0);

    private string Covers(BookListItemViewModel book) => _ctx.Render<BooksCoversView>(p => p.Add(c => c.Books, [book])).Markup;

    private string List(BookListItemViewModel book) => _ctx.Render<BooksListView>(p => p.Add(c => c.Books, [book])).Markup;

    [Fact]
    public void BooksCoversView_Should_MarkTheCover_OnlyWhenTheIsbnIsMissing()
    {
        Covers(Book(null, IsbnScanState.NotFound)).Should().Contain("No ISBN found in the pages");
        Covers(Book("9782800112343", IsbnScanState.HasIsbn)).Should().NotContain("cover-isbn-state");
    }

    [Fact]
    public void BooksListView_Should_ShowTheIsbnScanState_InFrontOfTheRating()
    {
        List(Book(null, IsbnScanState.NotScanned)).Should().Contain("No ISBN: the pages were not scanned yet");
        List(Book("9782800112343", IsbnScanState.HasIsbn)).Should().Contain("aria-label=\"ISBN 9782800112343\"");
    }
}

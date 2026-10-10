using Application.Books.IsbnScan;
using AwesomeAssertions;
using Bunit;
using Domain.Books;
using Domain.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Web.Components.Pages.Books;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Books;

public sealed class BookDetailIsbnScanTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly IBooksService _booksService = Substitute.For<IBooksService>();
    private readonly IIsbnScanService _isbnScanService = Substitute.For<IIsbnScanService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly DigitalBook _book = DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", null), Guid.CreateVersion7(), "/data/A/b.cbz", 1).Value!;

    public BookDetailIsbnScanTests()
    {
        _booksService.GetById(_book.Id.ToString()).Returns(Result<Book>.Success(_book));
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_booksService);
        _ctx.Services.AddSingleton(Substitute.For<IBookMoveWorkflow>());
        _ctx.Services.AddSingleton(_isbnScanService);
        _ctx.Services.AddSingleton(_snackbar);
    }

    public async ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        await _ctx.DisposeAsync();
    }

    private async Task<IRenderedComponent<BookDetail>> RenderAsync()
    {
        var cut = _ctx.Render<BookDetail>(p => p.Add(c => c.BookId, _book.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Âme rouge"));
        return cut;
    }

    private static Task ClickAsync(IRenderedComponent<BookDetail> cut, string text) =>
        cut.FindAll("button").Single(b => b.TextContent.Contains(text, StringComparison.Ordinal)).ClickAsync(new());

    private string CurrentUri => _ctx.Services.GetRequiredService<NavigationManager>().Uri;

    private void ScanReturns(Result<IsbnScanOutcome> result) =>
        _isbnScanService.ScanBookAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(result);

    private void ShouldShow(string message, Severity severity) =>
        _snackbar.Received(1).Add(message, severity, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());

    [Fact]
    public async Task LoadBookAsync_Should_OfferToScanThePages_WhenTheyWereNeverScanned()
    {
        var cut = await RenderAsync();

        cut.Markup.Should().Contain("The pages were not scanned for an ISBN yet.").And.Contain("Scan the pages");
    }

    [Fact]
    public async Task LoadBookAsync_Should_OfferToScanAgain_WhenThePagesShowedNoIsbn()
    {
        _book.RecordIsbnScan([], new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc));

        var cut = await RenderAsync();

        cut.Markup.Should().Contain("No ISBN found in the pages (scanned on").And.Contain("Scan again");
    }

    [Fact]
    public async Task LoadBookAsync_Should_NotShowTheIsbnSearch_WhenTheBookHasAnIsbn()
    {
        _book.AssignIsbn("9782800112343");

        var cut = await RenderAsync();

        cut.Markup.Should().NotContain("Scan the pages").And.NotContain("Several ISBNs are printed in this book");
    }

    [Fact]
    public async Task UseIsbnCandidate_Should_OpenTheWebImportWithThePickedIsbn_WhenSeveralIsbnsWereRead()
    {
        _book.RecordIsbnScan(["9782800112343", "2205056174"], DateTime.UtcNow);
        var cut = await RenderAsync();

        await ClickAsync(cut, "2205056174");

        CurrentUri.Should().EndWith($"/books/{_book.Id}/import?isbn=2205056174");
    }

    [Fact]
    public async Task FindIsbn_Should_OpenTheReaderInIsbnSearchMode()
    {
        var cut = await RenderAsync();

        await ClickAsync(cut, "Look in the reader");

        CurrentUri.Should().EndWith($"/books/{_book.Id}/read?mode=isbn");
    }

    [Fact]
    public async Task ScanIsbnAsync_Should_OpenTheWebImport_WhenTheIsbnWasFound()
    {
        ScanReturns(IsbnScanOutcome.Assigned);
        var cut = await RenderAsync();

        await ClickAsync(cut, "Scan the pages");

        ShouldShow("ISBN found in the pages", Severity.Success);
        CurrentUri.Should().EndWith($"/books/{_book.Id}/import");
    }

    [Theory]
    [InlineData(IsbnScanOutcome.WithCandidates, "Several ISBNs found in the pages: pick the book's own", Severity.Info)]
    [InlineData(IsbnScanOutcome.WithoutIsbn, "No ISBN found in the pages", Severity.Warning)]
    public async Task ScanIsbnAsync_Should_ReloadTheBook_WhenTheScanEnded(IsbnScanOutcome outcome, string message, Severity severity)
    {
        ScanReturns(outcome);
        var cut = await RenderAsync();

        await ClickAsync(cut, "Scan the pages");

        ShouldShow(message, severity);
        await _booksService.Received(2).GetById(_book.Id.ToString());
    }

    [Fact]
    public async Task ScanIsbnAsync_Should_ShowAnError_WhenThePagesCouldNotBeRead()
    {
        ScanReturns(IsbnScanOutcome.NotScanned);
        var cut = await RenderAsync();

        await ClickAsync(cut, "Scan the pages");

        ShouldShow("The pages could not be read", Severity.Error);
        await _booksService.Received(1).GetById(_book.Id.ToString());
    }

    [Fact]
    public async Task ScanIsbnAsync_Should_ShowAnError_WhenTheScanFails()
    {
        ScanReturns(BooksError.NotFound);
        var cut = await RenderAsync();

        await ClickAsync(cut, "Scan the pages");

        ShouldShow("Unable to scan the pages of this book", Severity.Error);
    }

    [Fact]
    public async Task ScanIsbnAsync_Should_ShowAnError_WhenTheScanThrows()
    {
        _isbnScanService.ScanBookAsync(_book.Id, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException());
        var cut = await RenderAsync();

        await ClickAsync(cut, "Scan the pages");

        ShouldShow("Unable to scan the pages of this book", Severity.Error);
    }
}

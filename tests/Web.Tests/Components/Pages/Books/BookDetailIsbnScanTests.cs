using Application.Books.IsbnOwners;
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
        OwnersReturn(Result<IReadOnlyList<IsbnOwnerDto>>.Success([]));
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_booksService);
        _ctx.Services.AddSingleton(Substitute.For<IBookMoveWorkflow>());
        _ctx.Services.AddSingleton(_isbnScanService);
        _ctx.Services.AddSingleton(Substitute.For<IBookReaderService>());
        _ctx.Services.AddSingleton(_snackbar);
        var librariesService = Substitute.For<ILibrariesService>();
        librariesService.GetById(Arg.Any<string?>()).Returns(Result<Domain.Libraries.Library>.Failure(Domain.Libraries.LibrariesError.NotFound));
        _ctx.Services.AddSingleton(librariesService);
        _ctx.Services.AddScoped<PopupAnchorLocator>();
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

    private void OwnersReturn(Result<IReadOnlyList<IsbnOwnerDto>> result) =>
        _booksService.GetIsbnCandidateOwners(_book.Id, Arg.Any<CancellationToken>()).Returns(result);

    private static IsbnOwnerDto OwnerOf(string isbn) => new(isbn, Guid.CreateVersion7(), "Blacksad", "Âme rouge", 4, "https://covers/blacksad.jpg", "Mangas");

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
    public async Task UseIsbnCandidate_Should_OpenFindBookDetailsWithThePickedIsbn_WhenSeveralIsbnsWereRead()
    {
        _book.RecordIsbnScan(["9782800112343", "2205056174"], DateTime.UtcNow);
        var cut = await RenderAsync();

        await ClickAsync(cut, "2205056174");

        CurrentUri.Should().EndWith($"/books/{_book.Id}/find-details?isbn=2205056174");
    }

    [Fact]
    public async Task FindIsbn_Should_OpenTheReaderInIsbnSearchMode()
    {
        var cut = await RenderAsync();

        await ClickAsync(cut, "Look in the reader");

        CurrentUri.Should().EndWith($"/books/{_book.Id}/read?mode=isbn&from=%2Fbooks%2F{_book.Id}");
    }

    [Fact]
    public async Task ScanIsbnAsync_Should_OpenFindBookDetails_WhenTheIsbnWasFound()
    {
        ScanReturns(IsbnScanOutcome.Assigned);
        var cut = await RenderAsync();

        await ClickAsync(cut, "Scan the pages");

        ShouldShow("ISBN found in the pages", Severity.Success);
        CurrentUri.Should().EndWith($"/books/{_book.Id}/find-details");
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

    [Fact]
    public async Task LoadIsbnCandidatesAsync_Should_ShowTheBookHoldingTheIsbn_WhenTheSingleIsbnReadIsUsed()
    {
        _book.RecordIsbnScan(["9782800112343"], DateTime.UtcNow);
        var owner = OwnerOf("9782800112343");
        OwnersReturn(Result<IReadOnlyList<IsbnOwnerDto>>.Success([owner]));

        var cut = await RenderAsync();

        cut.Markup.Should().Contain("This one may be a duplicate");
        cut.Find("a.book-isbn-owner").TextContent.Should().Contain("Blacksad · Volume 4").And.Contain("Library: Mangas");
        cut.FindAll("button").Should().NotContain(b => b.TextContent.Contains("9782800112343", StringComparison.Ordinal));
        cut.Find("a.book-isbn-owner").GetAttribute("href").Should().Be($"/books/{owner.BookId}?from=%2Fbooks%2F{_book.Id}");
        cut.Find("img.book-isbn-owner-cover").GetAttribute("alt").Should().Be("Cover of Âme rouge");
    }

    [Fact]
    public async Task LoadIsbnCandidatesAsync_Should_StillOfferTheIsbns_WhenTheirBooksCannotBeFound()
    {
        _book.RecordIsbnScan(["9782800112343", "2205056174"], DateTime.UtcNow);
        OwnersReturn(Result<IReadOnlyList<IsbnOwnerDto>>.Failure(BooksError.NotFound));

        var cut = await RenderAsync();

        ShouldShow(BookDetail.Msg_IsbnOwnersFailed, Severity.Error);
        cut.FindAll("button").Should().Contain(b => b.TextContent.Contains("9782800112343", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ScanIsbnAsync_Should_TellTheIsbnIsUsed_WhenTheSingleIsbnReadBelongsToAnotherBook()
    {
        _isbnScanService.ScanBookAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _book.RecordIsbnScan(["9782800112343"], DateTime.UtcNow);
            return Result<IsbnScanOutcome>.Success(IsbnScanOutcome.WithCandidates);
        });
        OwnersReturn(Result<IReadOnlyList<IsbnOwnerDto>>.Success([OwnerOf("9782800112343")]));
        var cut = await RenderAsync();

        await ClickAsync(cut, "Scan the pages");

        ShouldShow("The ISBN found in the pages is already used by another book", Severity.Info);
        cut.Markup.Should().Contain("Open that book");
    }
}

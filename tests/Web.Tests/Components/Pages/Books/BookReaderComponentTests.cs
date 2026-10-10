using Application.Books.Read;
using AwesomeAssertions;
using Bunit;
using Domain.Books;
using Domain.Errors;
using Domain.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages.Books;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Books;

public sealed class BookReaderComponentTests : IAsyncDisposable
{
    private readonly Guid _bookId = Guid.CreateVersion7();
    private readonly BunitContext _ctx = new();
    private readonly IBookReaderService _readerService = Substitute.For<IBookReaderService>();
    private readonly IBooksService _booksService = Substitute.For<IBooksService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly BunitJSModuleInterop _ocrModule;

    public BookReaderComponentTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./js/bookReader.js");
        _ocrModule = _ctx.JSInterop.SetupModule("./js/isbnOcr.js");
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_readerService);
        _ctx.Services.AddSingleton(_booksService);
        _ctx.Services.AddSingleton(_snackbar);
        _readerService.SaveProgressAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    public async ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        await _ctx.DisposeAsync();
    }

    private IRenderedComponent<BookReader> RenderReader(int pageCount, int lastReadPage)
    {
        _readerService.GetReaderInfoAsync(_bookId, Arg.Any<CancellationToken>())
            .Returns(new BookReaderInfoDto(_bookId, "Blacksad", "Arctic Nation", 2, pageCount, lastReadPage));
        return _ctx.Render<BookReader>(p => p.Add(c => c.BookId, _bookId.ToString()));
    }

    private static string CurrentPageSource(IRenderedComponent<BookReader> cut) =>
        cut.Find("img.reader-page").GetAttribute("src")!;

    [Fact]
    public async Task OnInitializedAsync_Should_OpenTheLastReadPage()
    {
        var cut = RenderReader(pageCount: 20, lastReadPage: 7);

        await cut.WaitForAssertionAsync(() => CurrentPageSource(cut).Should().Be($"/api/books/{_bookId}/pages/7"));
        cut.Markup.Should().Contain("8 / 20");
    }

    [Fact]
    public async Task NextPageAsync_Should_ShowTheFollowingPage()
    {
        var cut = RenderReader(pageCount: 20, lastReadPage: 7);
        await cut.WaitForAssertionAsync(() => cut.FindAll("img.reader-page").Should().ContainSingle());

        await cut.Find("button.reader-zone-next").ClickAsync(new());

        CurrentPageSource(cut).Should().Be($"/api/books/{_bookId}/pages/8");
    }

    [Fact]
    public async Task CloseAsync_Should_SaveTheCurrentPage()
    {
        var cut = RenderReader(pageCount: 20, lastReadPage: 7);
        await cut.WaitForAssertionAsync(() => cut.FindAll("img.reader-page").Should().ContainSingle());
        await cut.Find("button.reader-zone-previous").ClickAsync(new());

        await cut.Find("button[aria-label='Close reader']").ClickAsync(new());

        await _readerService.Received(1).SaveProgressAsync(_bookId, 6, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsReadAsync_Should_AddReadingDateAndRestartTheBook_WhenLastPageIsPassed()
    {
        _booksService.AddReadingDate(_bookId.ToString(), 3, Arg.Any<CancellationToken>()).Returns(Result<ReadingDate>.Success(null!));
        var cut = RenderReader(pageCount: 3, lastReadPage: 2);
        await cut.WaitForAssertionAsync(() => cut.FindAll("img.reader-page").Should().ContainSingle());

        await cut.Find("button.reader-zone-next").ClickAsync(new());
        cut.Markup.Should().Contain("You finished this book");

        await cut.FindAll("button").Single(b => b.TextContent.Contains("Mark as read", StringComparison.Ordinal)).ClickAsync(new());

        await _booksService.Received(1).AddReadingDate(_bookId.ToString(), 3, Arg.Any<CancellationToken>());
        await _readerService.Received(1).SaveProgressAsync(_bookId, 0, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnInitializedAsync_Should_ShowAnError_WhenTheBookCannotBeOpened()
    {
        _readerService.GetReaderInfoAsync(_bookId, Arg.Any<CancellationToken>())
            .Returns(Result<BookReaderInfoDto>.Failure(FileProcessingError.CorruptArchive));

        var cut = _ctx.Render<BookReader>(p => p.Add(c => c.BookId, _bookId.ToString()));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("This book cannot be read"));
        _snackbar.Received(1).Add(FileProcessingError.CorruptArchive.Description!, Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task OnNavigationKey_Should_TurnPagesAndClose()
    {
        var cut = RenderReader(pageCount: 20, lastReadPage: 7);
        await cut.WaitForAssertionAsync(() => cut.FindAll("img.reader-page").Should().ContainSingle());

        await cut.Instance.OnNavigationKey("ArrowRight");
        await cut.Instance.OnNavigationKey("ArrowRight");
        await cut.Instance.OnNavigationKey("ArrowLeft");
        await cut.Instance.OnNavigationKey("Enter");
        CurrentPageSource(cut).Should().Be($"/api/books/{_bookId}/pages/8");

        await cut.Instance.OnNavigationKey("Escape");

        await _readerService.Received(1).SaveProgressAsync(_bookId, 8, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GoToPageAsync_Should_SaveProgress_WhenReaderStaysOnAPage()
    {
        var cut = RenderReader(pageCount: 20, lastReadPage: 7);
        await cut.WaitForAssertionAsync(() => cut.FindAll("img.reader-page").Should().ContainSingle());

        await cut.Find("button.reader-zone-next").ClickAsync(new());

        // The save is debounced and triggers no render, so poll for it.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (_readerService.ReceivedCalls().All(c => c.GetMethodInfo().Name != nameof(IBookReaderService.SaveProgressAsync)) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        }

        await _readerService.Received(1).SaveProgressAsync(_bookId, 8, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleControls_Should_HideAndShowTheToolbars()
    {
        var cut = RenderReader(pageCount: 20, lastReadPage: 0);
        await cut.WaitForAssertionAsync(() => cut.FindAll(".reader-toolbar").Should().HaveCount(2));

        await cut.Find("button.reader-zone-toggle").ClickAsync(new());
        cut.FindAll(".reader-toolbar").Should().BeEmpty();

        await cut.Find("button.reader-zone-toggle").ClickAsync(new());
        cut.FindAll(".reader-toolbar").Should().HaveCount(2);
    }

    [Fact]
    public async Task MarkAsReadAsync_Should_ShowAnErrorAndStayOnTheBook_WhenAddingTheReadingDateFails()
    {
        _booksService.AddReadingDate(_bookId.ToString(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReadingDate>.Failure(BooksError.NotFound));
        var cut = RenderReader(pageCount: 3, lastReadPage: 2);
        await cut.WaitForAssertionAsync(() => cut.FindAll("img.reader-page").Should().ContainSingle());
        await cut.Find("button.reader-zone-next").ClickAsync(new());

        await cut.FindAll("button").Single(b => b.TextContent.Contains("Mark as read", StringComparison.Ordinal)).ClickAsync(new());

        _snackbar.Received(1).Add("Unexpected error while adding reading date", Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        await _readerService.DidNotReceive().SaveProgressAsync(_bookId, 0, Arg.Any<CancellationToken>());
        cut.Markup.Should().Contain("You finished this book");
    }

    [Fact]
    public async Task CloseFinish_Should_HideTheFinishPanel()
    {
        var cut = RenderReader(pageCount: 3, lastReadPage: 2);
        await cut.WaitForAssertionAsync(() => cut.FindAll("img.reader-page").Should().ContainSingle());
        await cut.Find("button.reader-zone-next").ClickAsync(new());

        await cut.FindAll("button").Single(b => b.TextContent.Contains("Keep reading", StringComparison.Ordinal)).ClickAsync(new());

        cut.Markup.Should().NotContain("You finished this book");
    }

    [Fact]
    public async Task OnInitializedAsync_Should_ShowAnError_WhenBookIdIsInvalid()
    {
        var cut = _ctx.Render<BookReader>(p => p.Add(c => c.BookId, "not-a-guid"));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("This book cannot be read"));
        await _readerService.DidNotReceive().GetReaderInfoAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private async Task<IRenderedComponent<BookReader>> ExtractIsbnAsync(string recognizedText)
    {
        _ocrModule.Setup<string>("recognize", _ => true).SetResult(recognizedText);
        var cut = RenderReader(pageCount: 20, lastReadPage: 1);
        await cut.WaitForAssertionAsync(() => cut.FindAll("img.reader-page").Should().ContainSingle());

        await cut.Find("button[aria-label='Find the ISBN on this page']").ClickAsync(new());
        return cut;
    }

    private static IEnumerable<AngleSharp.Dom.IElement> IsbnButtons(IRenderedComponent<BookReader> cut) =>
        cut.FindAll("button.reader-isbn");

    [Fact]
    public async Task ExtractIsbnAsync_Should_ShowTheIsbnsReadOnThePage()
    {
        var cut = await ExtractIsbnAsync("Dépôt légal : mars 2024\nISBN 978-2-8001-1234-3\nIntégrale : 2-205-05617-4");

        IsbnButtons(cut).Select(b => b.TextContent.Trim()).Should().Equal("9782800112343", "2205056174");
    }

    [Fact]
    public async Task ExtractIsbnAsync_Should_WarnTheReader_WhenThePageHasNoIsbn()
    {
        var cut = await ExtractIsbnAsync("Chapitre 1");

        IsbnButtons(cut).Should().BeEmpty();
        _snackbar.Received(1).Add("No ISBN found on this page", Severity.Warning, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ExtractIsbnAsync_Should_ShowAnError_WhenTheOcrFails()
    {
        _ocrModule.Setup<string>("recognize", _ => true).SetException(new JSException("Unable to load the OCR library"));
        var cut = RenderReader(pageCount: 20, lastReadPage: 1);
        await cut.WaitForAssertionAsync(() => cut.FindAll("img.reader-page").Should().ContainSingle());

        await cut.Find("button[aria-label='Find the ISBN on this page']").ClickAsync(new());

        _snackbar.Received(1).Add("Unable to read the text of this page", Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        cut.FindAll("button[aria-label='Find the ISBN on this page']").Should().ContainSingle();
    }

    [Fact]
    public async Task ExtractIsbnAsync_Should_IgnoreTheText_WhenThePageWasTurnedMeanwhile()
    {
        var recognition = _ocrModule.Setup<string>("recognize", _ => true);
        var cut = RenderReader(pageCount: 20, lastReadPage: 1);
        await cut.WaitForAssertionAsync(() => cut.FindAll("img.reader-page").Should().ContainSingle());
        await cut.Find("button[aria-label='Find the ISBN on this page']").ClickAsync(new());
        await cut.Find("button.reader-zone-next").ClickAsync(new());

        await cut.InvokeAsync(() => recognition.SetResult("ISBN 978-2-8001-1234-3"));

        await cut.WaitForAssertionAsync(() => cut.FindAll("button[aria-label='Find the ISBN on this page']").Should().ContainSingle());
        IsbnButtons(cut).Should().BeEmpty();
    }

    [Fact]
    public async Task FetchBookInfoAsync_Should_OpenTheWebImportWithTheSelectedIsbn()
    {
        var cut = await ExtractIsbnAsync("ISBN 978-2-8001-1234-3");

        await IsbnButtons(cut).Single().ClickAsync(new());

        _ctx.Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith($"/books/{_bookId}/import?isbn=9782800112343");
    }

    [Fact]
    public async Task CloseIsbnCandidates_Should_HideTheIsbnPanel()
    {
        var cut = await ExtractIsbnAsync("ISBN 978-2-8001-1234-3");

        await cut.FindAll("button").Single(b => b.TextContent.Contains("Cancel", StringComparison.Ordinal)).ClickAsync(new());

        IsbnButtons(cut).Should().BeEmpty();
    }

    private async Task<IRenderedComponent<BookReader>> RenderIsbnSearchAsync(int pageCount, int lastReadPage)
    {
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/books/{_bookId}/read?mode=isbn");
        var cut = RenderReader(pageCount, lastReadPage);
        await cut.WaitForAssertionAsync(() => cut.FindAll("img.reader-page").Should().ContainSingle());
        return cut;
    }

    private static Task GoToQuickPageAsync(IRenderedComponent<BookReader> cut, int pageNumber) =>
        cut.Find($"button[aria-label='Go to page {pageNumber}']").ClickAsync(new());

    [Fact]
    public async Task OnInitializedAsync_Should_OpenTheFirstPageWithShortcutsToBothEnds_WhenSearchingTheIsbn()
    {
        var cut = await RenderIsbnSearchAsync(pageCount: 20, lastReadPage: 7);

        CurrentPageSource(cut).Should().Be($"/api/books/{_bookId}/pages/0");
        cut.FindAll("button.reader-quick-page").Select(b => b.TextContent.Trim())
            .Should().Equal("1", "2", "3", "4", "5", "16", "17", "18", "19", "20");
    }

    [Fact]
    public async Task CloseAsync_Should_NotSaveTheProgress_WhenSearchingTheIsbn()
    {
        var cut = await RenderIsbnSearchAsync(pageCount: 20, lastReadPage: 7);
        await GoToQuickPageAsync(cut, 19);

        await cut.Find("button[aria-label='Close reader']").ClickAsync(new());

        CurrentPageSource(cut).Should().Be($"/api/books/{_bookId}/pages/18");
        await _readerService.DidNotReceive().SaveProgressAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NextPageAsync_Should_NotShowTheFinishPanel_WhenSearchingTheIsbn()
    {
        var cut = await RenderIsbnSearchAsync(pageCount: 20, lastReadPage: 0);
        await GoToQuickPageAsync(cut, 20);

        await cut.Find("button.reader-zone-next").ClickAsync(new());

        cut.Markup.Should().NotContain("You finished this book");
    }

    [Fact]
    public async Task ScanIsbnPagesAsync_Should_ShowThePageWhereTheIsbnIsPrinted_WithoutSavingTheProgress()
    {
        _ocrModule.Setup<string>("recognizeUrl", call => Equals(call.Arguments[0], $"/api/books/{_bookId}/pages/18"))
            .SetResult("ISBN 978-2-8001-1234-3");
        var cut = await RenderIsbnSearchAsync(pageCount: 20, lastReadPage: 7);

        await cut.Find("button[aria-label='Scan the first and last pages']").ClickAsync(new());
        await cut.WaitForAssertionAsync(() => IsbnButtons(cut).Should().ContainSingle());
        await IsbnButtons(cut).Single().ClickAsync(new());

        CurrentPageSource(cut).Should().Be($"/api/books/{_bookId}/pages/18");
        _ctx.Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith($"/books/{_bookId}/import?isbn=9782800112343");
        await _readerService.DidNotReceive().SaveProgressAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScanIsbnPagesAsync_Should_WarnTheReader_WhenNoPageHasAnIsbn()
    {
        _ocrModule.Setup<string>("recognizeUrl", _ => true).SetResult("Chapitre 1");
        var cut = await RenderIsbnSearchAsync(pageCount: 20, lastReadPage: 0);

        await cut.Find("button[aria-label='Scan the first and last pages']").ClickAsync(new());

        await cut.WaitForAssertionAsync(() =>
            _snackbar.Received(1).Add("No ISBN found in the first and last pages", Severity.Warning, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>()));
        _ocrModule.Invocations["recognizeUrl"].Should().HaveCount(10);
    }

    [Fact]
    public async Task ScanIsbnPagesAsync_Should_ShowAnError_WhenTheOcrFails()
    {
        _ocrModule.Setup<string>("recognizeUrl", _ => true).SetException(new JSException("Unable to load the OCR library"));
        var cut = await RenderIsbnSearchAsync(pageCount: 20, lastReadPage: 0);

        await cut.Find("button[aria-label='Scan the first and last pages']").ClickAsync(new());

        await cut.WaitForAssertionAsync(() =>
            _snackbar.Received(1).Add("Unable to read the text of the pages", Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>()));
    }
}

using System.Globalization;
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
using Web.Infrastructure;
using Web.Models;
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
    private readonly BunitJSModuleInterop _readerModule;
    private readonly BunitJSModuleInterop _ocrModule;

    public BookReaderComponentTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _readerModule = _ctx.JSInterop.SetupModule(AppVersion.Versioned("./js/bookReader.js"));
        _ocrModule = _ctx.JSInterop.SetupModule(AppVersion.Versioned("./js/isbnOcr.js"));
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

    private IRenderedComponent<BookReader> RenderReader(int pageCount, int lastReadPage, ReaderSettings? settings = null)
    {
        _readerService.GetReaderInfoAsync(_bookId, Arg.Any<CancellationToken>())
            .Returns(new BookReaderInfoDto(_bookId, "Blacksad", "Arctic Nation", 2, pageCount, lastReadPage, settings ?? ReaderSettings.Default));
        return _ctx.Render<BookReader>(p => p.Add(c => c.BookId, _bookId.ToString()));
    }

    private async Task<IRenderedComponent<BookReader>> RenderAttachedReaderAsync(int pageCount, int lastReadPage, ReaderSettings? settings = null)
    {
        var cut = RenderReader(pageCount, lastReadPage, settings);
        await WaitForAttachAsync(cut);
        return cut;
    }

    private Task WaitForAttachAsync(IRenderedComponent<BookReader> cut) =>
        cut.WaitForAssertionAsync(() => _readerModule.Invocations.Should().Contain(i => i.Identifier == "attach"));

    private static string Counter(IRenderedComponent<BookReader> cut) =>
        cut.Find(".reader-toolbar-bottom .reader-counter").TextContent.Trim();

    private static int[] RequestedPages(IRenderedComponent<BookReader> cut) =>
        cut.FindAll(".reader-page:not(.reader-page-pending)").Select(p => int.Parse(p.GetAttribute("data-page")!, CultureInfo.InvariantCulture)).ToArray();

    private int[] ScrolledRows() =>
        _readerModule.Invocations.Where(i => i.Identifier == "scrollToRow").Select(i => (int)i.Arguments[0]!).ToArray();

    private static Task ClickAsync(IRenderedComponent<BookReader> cut, string ariaLabel) =>
        cut.Find($"button[aria-label='{ariaLabel}']").ClickAsync(new());

    private static Task ClickButtonWithTextAsync(IRenderedComponent<BookReader> cut, string text) =>
        cut.FindAll("button").Single(b => b.TextContent.Contains(text, StringComparison.Ordinal)).ClickAsync(new());

    [Fact]
    public async Task OnInitializedAsync_Should_OpenTheLastReadPage_AndLoadOnlyItsNeighbours()
    {
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 7);

        Counter(cut).Should().Be("8 / 20");
        _readerModule.Invocations["attach"].Single().Arguments[2].Should().Be(7);
        cut.FindAll(".reader-page").Should().HaveCount(20);
        RequestedPages(cut).Should().Equal(6, 7, 8, 9, 10);
        cut.Find("img[alt='Page 8']").GetAttribute("src").Should().Be($"/api/books/{_bookId}/pages/7");
    }

    [Fact]
    public async Task NextPageAsync_Should_ScrollToTheFollowingPage()
    {
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 7);

        await ClickAsync(cut, "Next page");

        ScrolledRows().Should().Equal(8);
        Counter(cut).Should().Be("9 / 20");
        RequestedPages(cut).Should().Contain(11);
    }

    [Fact]
    public async Task GoToPageAsync_Should_StayOnThePage_WhenTheScrollFails()
    {
        _readerModule.SetupVoid("scrollToRow", _ => true).SetException(new JSException("The reader is gone"));
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 7);

        await ClickAsync(cut, "Next page");

        Counter(cut).Should().Be("8 / 20");
    }

    [Fact]
    public async Task OnVisibleRowChanged_Should_FollowThePageBeingRead()
    {
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 7);

        await cut.Instance.OnVisibleRowChanged(12);
        await cut.Instance.OnVisibleRowChanged(-1);
        await cut.Instance.OnVisibleRowChanged(20);

        Counter(cut).Should().Be("13 / 20");
        RequestedPages(cut).Should().Equal(6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
        ScrolledRows().Should().BeEmpty();
    }

    [Fact]
    public async Task CloseAsync_Should_SaveTheCurrentPage()
    {
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 7);
        await ClickAsync(cut, "Previous page");

        await ClickAsync(cut, "Close reader");

        await _readerService.Received(1).SaveProgressAsync(_bookId, 6, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsReadAsync_Should_AddReadingDateAndRestartTheBook_WhenLastPageIsPassed()
    {
        _booksService.AddReadingDate(_bookId.ToString(), 3, Arg.Any<CancellationToken>()).Returns(Result<ReadingDate>.Success(null!));
        var cut = await RenderAttachedReaderAsync(pageCount: 3, lastReadPage: 2);

        await ClickAsync(cut, "Next page");
        cut.Markup.Should().Contain("You finished this book");

        await ClickButtonWithTextAsync(cut, "Mark as read");

        await _booksService.Received(1).AddReadingDate(_bookId.ToString(), 3, Arg.Any<CancellationToken>());
        await _readerService.Received(1).SaveProgressAsync(_bookId, 0, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnReachedEnd_Should_ShowTheFinishPanel()
    {
        var cut = await RenderAttachedReaderAsync(pageCount: 3, lastReadPage: 0);

        await cut.Instance.OnReachedEnd();

        cut.Markup.Should().Contain("You finished this book");
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
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 7);

        await cut.Instance.OnNavigationKey("ArrowRight");
        await cut.Instance.OnNavigationKey("ArrowRight");
        await cut.Instance.OnNavigationKey("ArrowLeft");
        await cut.Instance.OnNavigationKey("Enter");
        Counter(cut).Should().Be("9 / 20");

        await cut.Instance.OnNavigationKey("Escape");

        await _readerService.Received(1).SaveProgressAsync(_bookId, 8, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GoToPageAsync_Should_SaveProgress_WhenReaderStaysOnAPage()
    {
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 7);

        await ClickAsync(cut, "Next page");

        // The save is debounced and triggers no render, so poll for it.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (_readerService.ReceivedCalls().All(c => c.GetMethodInfo().Name != nameof(IBookReaderService.SaveProgressAsync)) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        }

        await _readerService.Received(1).SaveProgressAsync(_bookId, 8, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnCenterTap_Should_SwapTheToolbarsForTheProgressLine()
    {
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 4);
        cut.FindAll(".reader-toolbar").Should().HaveCount(2);

        await cut.Instance.OnCenterTap();
        cut.FindAll(".reader-toolbar").Should().BeEmpty();
        cut.Find(".reader-progress-value").GetAttribute("style").Should().Contain("width: 25%");

        await cut.Instance.OnCenterTap();
        cut.FindAll(".reader-toolbar").Should().HaveCount(2);
    }

    [Fact]
    public async Task ToggleFullscreenAsync_Should_AskTheBrowser_WhenItSupportsFullScreen()
    {
        _readerModule.Setup<ReaderClientState>("attach", _ => true).SetResult(new ReaderClientState(CanUseFullscreen: true, GestureHelpSeen: true));
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 0);

        await cut.WaitForAssertionAsync(() => cut.FindAll("button[aria-label='Full screen']").Should().ContainSingle());
        await ClickAsync(cut, "Full screen");

        _readerModule.Invocations.Should().Contain(i => i.Identifier == "toggleFullscreen");
    }

    [Fact]
    public async Task OnAfterRenderAsync_Should_HideTheFullScreenButton_WhenTheBrowserDoesNotSupportIt()
    {
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 0);

        cut.FindAll("button[aria-label='Full screen']").Should().BeEmpty();
    }

    [Fact]
    public async Task MarkAsReadAsync_Should_ShowAnErrorAndStayOnTheBook_WhenAddingTheReadingDateFails()
    {
        _booksService.AddReadingDate(_bookId.ToString(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReadingDate>.Failure(BooksError.NotFound));
        var cut = await RenderAttachedReaderAsync(pageCount: 3, lastReadPage: 2);
        await ClickAsync(cut, "Next page");

        await ClickButtonWithTextAsync(cut, "Mark as read");

        _snackbar.Received(1).Add("Unexpected error while adding reading date", Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        await _readerService.DidNotReceive().SaveProgressAsync(_bookId, 0, Arg.Any<CancellationToken>());
        cut.Markup.Should().Contain("You finished this book");
    }

    [Fact]
    public async Task CloseFinish_Should_HideTheFinishPanel()
    {
        var cut = await RenderAttachedReaderAsync(pageCount: 3, lastReadPage: 2);
        await ClickAsync(cut, "Next page");

        await ClickButtonWithTextAsync(cut, "Keep reading");

        cut.Markup.Should().NotContain("You finished this book");
    }

    private static readonly ReaderSettings s_manga = new(ReaderLayout.FitWidth, ReaderScrolling.Continuous, ReadingDirection.RightToLeft);
    private static readonly ReaderSettings s_pageByPage = new(ReaderLayout.SinglePage, ReaderScrolling.PageByPage, ReadingDirection.LeftToRight);

    private static int[] DisplayedRows(IRenderedComponent<BookReader> cut) =>
        cut.FindAll(".reader-row").Select(r => int.Parse(r.GetAttribute("data-row")!, CultureInfo.InvariantCulture)).ToArray();

    private static async Task ChooseDisplayOptionAsync(IRenderedComponent<BookReader> cut, string label)
    {
        await ClickAsync(cut, "Display");
        await cut.FindAll("button.reader-option").Single(b => b.TextContent.Contains(label, StringComparison.Ordinal)).ClickAsync(new());
    }

    [Fact]
    public async Task OnAfterRenderAsync_Should_TellTheScriptHowTheBookIsDisplayed()
    {
        await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 7, s_manga);

        var attach = _readerModule.Invocations["attach"].Single();
        attach.Arguments[3].Should().Be(ReaderScriptOptions.From(s_manga));
    }

    [Fact]
    public async Task ApplySettingsAsync_Should_ShowTheSamePageInTheNewLayout_AndSaveTheSettings()
    {
        _readerService.SaveSettingsAsync(_bookId, Arg.Any<ReaderSettings>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 4);

        await ChooseDisplayOptionAsync(cut, "Two pages");
        var expected = new ReaderSettings(ReaderLayout.TwoPages, ReaderScrolling.Continuous, ReadingDirection.LeftToRight);

        Counter(cut).Should().Be("4-5 / 20");
        cut.Find(".reader-scroll").ClassList.Should().Contain(["reader-layout-two", "reader-snap"]);
        await cut.WaitForAssertionAsync(() => _readerModule.Invocations.Should().Contain(i => i.Identifier == "configure"));
        _readerModule.Invocations["configure"].Single().Arguments.Should().Equal(ReaderScriptOptions.From(expected), 2);
        cut.Find(".reader-scroll").GetAttribute("data-layout").Should().Be("TwoPages-Continuous-LeftToRight");
        await _readerService.Received(1).SaveSettingsAsync(_bookId, expected, Arg.Any<CancellationToken>());
        cut.Find("button.reader-option-selected[aria-pressed='true']").TextContent.Should().Contain("Two pages");
    }

    [Fact]
    public async Task ApplySettingsAsync_Should_WarnTheReader_WhenTheSettingsCannotBeSaved()
    {
        _readerService.SaveSettingsAsync(_bookId, Arg.Any<ReaderSettings>(), Arg.Any<CancellationToken>()).Returns(Result.Failure(BooksError.NotFound));
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 4);

        await ChooseDisplayOptionAsync(cut, "Right to left");

        _snackbar.Received(1).Add("Unable to save the display settings", Severity.Warning, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        cut.Find(".reader-direction").TextContent.Should().Contain("Right to left");
    }

    [Fact]
    public async Task ApplySettingsAsync_Should_NotSave_WhenTheChosenOptionIsAlreadySelected()
    {
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 4);

        await ChooseDisplayOptionAsync(cut, "Fit width");
        await ClickAsync(cut, "Close the display settings");

        await _readerService.DidNotReceive().SaveSettingsAsync(Arg.Any<Guid>(), Arg.Any<ReaderSettings>(), Arg.Any<CancellationToken>());
        cut.FindAll(".reader-panel").Should().BeEmpty();
    }

    [Fact]
    public async Task OnNavigationKey_Should_TurnPagesTheOtherWay_WhenReadingRightToLeft()
    {
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 7, s_manga);

        await cut.Instance.OnNavigationKey("ArrowLeft");
        Counter(cut).Should().Be("9 / 20");

        await cut.Instance.OnNavigationKey("ArrowRight");
        await cut.Instance.OnNavigationKey("ArrowRight");
        Counter(cut).Should().Be("7 / 20");
        cut.Find(".reader-slider").GetAttribute("dir").Should().Be("rtl");
    }

    [Fact]
    public async Task OnTurnPage_Should_ShowOnlyTheNextPage_WhenReadingPageByPage()
    {
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 7, s_pageByPage);
        DisplayedRows(cut).Should().Equal(7);
        cut.Find("img.reader-preload").GetAttribute("src").Should().Be($"/api/books/{_bookId}/pages/8");

        await cut.Instance.OnTurnPage(1);

        DisplayedRows(cut).Should().Equal(8);
        Counter(cut).Should().Be("9 / 20");
        await cut.WaitForAssertionAsync(() => ScrolledRows().Should().Equal(8));

        await cut.Instance.OnTurnPage(-1);
        DisplayedRows(cut).Should().Equal(7);
    }

    [Fact]
    public async Task OnTurnPage_Should_ShowTheFinishPanel_AfterTheLastPage()
    {
        var cut = await RenderAttachedReaderAsync(pageCount: 3, lastReadPage: 2, s_pageByPage);

        await cut.Instance.OnTurnPage(1);

        cut.Markup.Should().Contain("You finished this book");
        cut.FindAll("img.reader-preload").Should().BeEmpty();
    }

    [Fact]
    public async Task OnAfterRenderAsync_Should_ShowTheGestureHelp_OnlyUntilItIsDismissed()
    {
        _readerModule.Setup<ReaderClientState>("attach", _ => true).SetResult(new ReaderClientState(CanUseFullscreen: false, GestureHelpSeen: false));
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 0);
        await cut.WaitForAssertionAsync(() => cut.FindAll(".reader-help").Should().ContainSingle());
        cut.Find(".reader-help").TextContent.Should().Contain("scroll freely");

        await ClickButtonWithTextAsync(cut, "Got it");

        cut.FindAll(".reader-help").Should().BeEmpty();
        _readerModule.Invocations.Should().Contain(i => i.Identifier == "markGestureHelpSeen");
    }

    [Fact]
    public async Task OnAfterRenderAsync_Should_NotShowTheGestureHelp_WhenItWasAlreadySeen()
    {
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 0);

        cut.FindAll(".reader-help").Should().BeEmpty();
    }

    [Fact]
    public async Task OnInitializedAsync_Should_ShowAnError_WhenBookIdIsInvalid()
    {
        var cut = _ctx.Render<BookReader>(p => p.Add(c => c.BookId, "not-a-guid"));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("This book cannot be read"));
        await _readerService.DidNotReceive().GetReaderInfoAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static async Task FindIsbnOnThePageAsync(IRenderedComponent<BookReader> cut, IRenderedComponent<MudPopoverProvider> provider)
    {
        await ClickAsync(cut, "More actions");
        await provider.WaitForElement(".mud-menu-item:contains('Find the ISBN on this page')").ClickAsync(new());
    }

    private async Task<IRenderedComponent<BookReader>> ExtractIsbnAsync(string recognizedText)
    {
        _ocrModule.Setup<string>("recognizeUrl", call => Equals(call.Arguments[0], $"/api/books/{_bookId}/pages/1"))
            .SetResult(recognizedText);
        var provider = _ctx.Render<MudPopoverProvider>();
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 1);

        await FindIsbnOnThePageAsync(cut, provider);
        return cut;
    }

    private static IEnumerable<AngleSharp.Dom.IElement> IsbnButtons(IRenderedComponent<BookReader> cut) =>
        cut.FindAll("button.reader-isbn");

    [Fact]
    public async Task ExtractIsbnAsync_Should_ShowTheIsbnsReadOnThePage()
    {
        var cut = await ExtractIsbnAsync("Dépôt légal : mars 2024\nISBN 978-2-8001-1234-3\nIntégrale : 2-205-05617-4");

        await cut.WaitForAssertionAsync(() => IsbnButtons(cut).Select(b => b.TextContent.Trim()).Should().Equal("9782800112343", "2205056174"));
    }

    [Fact]
    public async Task ExtractIsbnAsync_Should_WarnTheReader_WhenThePageHasNoIsbn()
    {
        var cut = await ExtractIsbnAsync("Chapitre 1");

        await cut.WaitForAssertionAsync(() =>
            _snackbar.Received(1).Add("No ISBN found on this page", Severity.Warning, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>()));
        IsbnButtons(cut).Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractIsbnAsync_Should_ShowAnError_WhenTheOcrFails()
    {
        _ocrModule.Setup<string>("recognizeUrl", _ => true).SetException(new JSException("Unable to load the OCR library"));
        var provider = _ctx.Render<MudPopoverProvider>();
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 1);

        await FindIsbnOnThePageAsync(cut, provider);

        await cut.WaitForAssertionAsync(() =>
            _snackbar.Received(1).Add("Unable to read the text of this page", Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>()));
    }

    [Fact]
    public async Task ExtractIsbnAsync_Should_IgnoreTheText_WhenThePageWasTurnedMeanwhile()
    {
        var recognition = _ocrModule.Setup<string>("recognizeUrl", _ => true);
        var provider = _ctx.Render<MudPopoverProvider>();
        var cut = await RenderAttachedReaderAsync(pageCount: 20, lastReadPage: 1);
        var extraction = FindIsbnOnThePageAsync(cut, provider);
        await cut.WaitForAssertionAsync(() => _ocrModule.Invocations.Should().Contain(i => i.Identifier == "recognizeUrl"));
        await cut.Instance.OnVisibleRowChanged(2);

        await cut.InvokeAsync(() => recognition.SetResult("ISBN 978-2-8001-1234-3"));
        await extraction;

        await cut.WaitForAssertionAsync(() => cut.FindAll(".mud-progress-circular").Should().BeEmpty());
        IsbnButtons(cut).Should().BeEmpty();
    }

    [Fact]
    public async Task FetchBookInfoAsync_Should_OpenFindBookDetailsWithTheSelectedIsbn()
    {
        var cut = await ExtractIsbnAsync("ISBN 978-2-8001-1234-3");

        await IsbnButtons(cut).Single().ClickAsync(new());

        _ctx.Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith($"/books/{_bookId}/find-details?isbn=9782800112343");
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
        return await RenderAttachedReaderAsync(pageCount, lastReadPage);
    }

    private static Task GoToQuickPageAsync(IRenderedComponent<BookReader> cut, int pageNumber) =>
        cut.Find($"button[aria-label='Go to page {pageNumber}']").ClickAsync(new());

    [Fact]
    public async Task OnInitializedAsync_Should_OpenTheFirstPageWithShortcutsToBothEnds_WhenSearchingTheIsbn()
    {
        var cut = await RenderIsbnSearchAsync(pageCount: 20, lastReadPage: 7);

        Counter(cut).Should().Be("1 / 20");
        cut.FindAll("button.reader-quick-page").Select(b => b.TextContent.Trim())
            .Should().Equal("1", "2", "3", "4", "5", "16", "17", "18", "19", "20");
    }

    [Fact]
    public async Task CloseAsync_Should_NotSaveTheProgress_WhenSearchingTheIsbn()
    {
        var cut = await RenderIsbnSearchAsync(pageCount: 20, lastReadPage: 7);
        await GoToQuickPageAsync(cut, 19);

        await cut.Find("button[aria-label='Close reader']").ClickAsync(new());

        Counter(cut).Should().Be("19 / 20");
        await _readerService.DidNotReceive().SaveProgressAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NextPageAsync_Should_NotShowTheFinishPanel_WhenSearchingTheIsbn()
    {
        var cut = await RenderIsbnSearchAsync(pageCount: 20, lastReadPage: 0);
        await GoToQuickPageAsync(cut, 20);

        await ClickAsync(cut, "Next page");

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

        ScrolledRows().Should().Equal(18);
        _ctx.Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith($"/books/{_bookId}/find-details?isbn=9782800112343");
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

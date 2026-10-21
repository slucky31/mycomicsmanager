using Application.Books.List;
using Application.Interfaces;
using AwesomeAssertions;
using Bunit;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages.Libraries;
using Web.Components.Pages.Libraries.Views;
using Web.Enums;
using Web.Infrastructure;
using Web.Models;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Libraries;

public sealed class LibraryDetailPageTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly ILibrariesService _librariesService = Substitute.For<ILibrariesService>();
    private readonly IBooksService _booksService = Substitute.For<IBooksService>();
    private readonly IBookMoveWorkflow _workflow = Substitute.For<IBookMoveWorkflow>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly LibraryStateService _stateService = new();
    private readonly BunitJSModuleInterop _module;
    private readonly BookSummaryDto _blacksad = Book("Âme rouge", "Blacksad", 4);
    private readonly BookSummaryDto _hiver = Book("Hiver nucléaire", "Hiver nucléaire", 2);
    private Library _library = Library.Create("Read Books", "#5C6BC0", "Bookmark", LibraryBookType.Physical, Guid.CreateVersion7()).Value!;

    public LibraryDetailPageTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _module = _ctx.JSInterop.SetupModule(AppVersion.Versioned("./js/libraryPage.js"));
        _module.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_librariesService);
        _ctx.Services.AddSingleton(_booksService);
        _ctx.Services.AddSingleton(_workflow);
        _ctx.Services.AddSingleton(_stateService);
        _ctx.Services.AddSingleton(_snackbar);
        _ctx.Services.AddSingleton(Substitute.For<IIsbnScanService>());
    }

    public async ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        await _ctx.DisposeAsync();
    }

    private static BookSummaryDto Book(string title, string serie, int volume) =>
        new() { Id = Guid.CreateVersion7(), Title = title, Serie = serie, VolumeNumber = volume, ISBN = "9782800112343" };

    private static IPagedList<BookSummaryDto> Page(int totalCount, bool hasNextPage, params BookSummaryDto[] books)
    {
        var page = Substitute.For<IPagedList<BookSummaryDto>>();
        page.Items.Returns(books);
        page.TotalCount.Returns(totalCount);
        page.HasNextPage.Returns(hasNextPage);
        return page;
    }

    private void GivenPage(int pageNumber, string search, Task<Result<IPagedList<BookSummaryDto>>> page) =>
        _booksService.GetPagedByLibrary(_library.Id, pageNumber, LibraryDetailPage.PageSize, Arg.Any<BookSortOrder>(), search, Arg.Any<CancellationToken>())
            .Returns(page);

    private void GivenPage(int pageNumber, string search, IPagedList<BookSummaryDto> page) =>
        GivenPage(pageNumber, search, Task.FromResult(Result<IPagedList<BookSummaryDto>>.Success(page)));

    private async Task<IRenderedComponent<LibraryDetailPage>> RenderAsync(LibraryBookType bookType = LibraryBookType.Physical)
    {
        _library = Library.Create("Read Books", "#5C6BC0", "Bookmark", bookType, Guid.CreateVersion7()).Value!;
        _librariesService.GetById(_library.Id.ToString()).Returns(Result<Library>.Success(_library));
        _librariesService.Update(Arg.Any<UpdateLibraryRequest>(), Arg.Any<CancellationToken>()).Returns(Result<Library>.Success(_library));
        GivenPage(1, string.Empty, Page(24, false, _blacksad, _hiver));

        var cut = _ctx.Render<LibraryDetailPage>(p => p.Add(c => c.LibraryId, _library.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.FindAll(".cover-item").Should().HaveCount(2));
        return cut;
    }

    private static string Subtitle(IRenderedComponent<LibraryDetailPage> cut) => cut.Find(".library-bar-subtitle").TextContent.Trim();

    private static async Task TypeSearchAsync(IRenderedComponent<LibraryDetailPage> cut, string search) =>
        await cut.Find("input[type=search]").InputAsync(new() { Value = search });

    private static async Task ChooseAsync(IRenderedComponent<LibraryDetailPage> cut, string sheetButton, string option)
    {
        await cut.Find($"button[aria-label='{sheetButton}']").ClickAsync(new());
        await cut.Find($"[role=dialog] .action-sheet-item:contains('{option}')").ClickAsync(new());
    }

    [Fact]
    public async Task LibraryDetailPage_Should_ShowTheNumberOfBooksAndTheSortOrder_InASingleBar()
    {
        var cut = await RenderAsync();

        cut.Find("h1").TextContent.Should().Be("Read Books");
        Subtitle(cut).Should().Be("24 books · Newest first · Covers");
        _module.VerifyInvoke("attach");
    }

    [Fact]
    public async Task OnParametersSetAsync_Should_ShowTheOtherLibrary_WhenTheSideBarSwitchesLibrary()
    {
        var cut = await RenderAsync();
        var first = _library;
        await TypeSearchAsync(cut, "Blacksad");
        await cut.Find("button[aria-label='Sort and view']").ClickAsync(new());
        var other = Library.Create("Comics", "#2E7D32", "Star", LibraryBookType.Digital, Guid.CreateVersion7()).Value!;
        _librariesService.GetById(other.Id.ToString()).Returns(Result<Library>.Success(other));
        var otherPage = Page(1, false, _hiver);
        _booksService.GetPagedByLibrary(other.Id, 1, LibraryDetailPage.PageSize, Arg.Any<BookSortOrder>(), string.Empty, Arg.Any<CancellationToken>())
            .Returns(Result<IPagedList<BookSummaryDto>>.Success(otherPage));

        cut.Render(p => p.Add(c => c.LibraryId, other.Id.ToString()));

        await cut.WaitForAssertionAsync(() => cut.Find("h1").TextContent.Should().Be("Comics"));
        cut.FindAll(".cover-item").Should().ContainSingle();
        cut.FindAll("[role=dialog]").Should().BeEmpty();
        cut.Find("input[type=search]").GetAttribute("value").Should().BeNullOrEmpty();
        _stateService.Load(first.Id)!.Search.Should().Be("Blacksad");
        _module.VerifyInvoke("scrollToTop");
    }

    [Fact]
    public async Task OnParametersSetAsync_Should_KeepTheBooksOnScreen_WhenTheSameLibraryIsShownAgain()
    {
        var cut = await RenderAsync();

        cut.Render(p => p.Add(c => c.LibraryId, _library.Id.ToString()));

        await _librariesService.Received(1).GetById(_library.Id.ToString());
        cut.FindAll(".cover-item").Should().HaveCount(2);
    }

    [Fact]
    public async Task OnParametersSetAsync_Should_ShowTheOtherLibrary_AfterASort_EvenWhenItCannotScrollBackToTheTop()
    {
        var cut = await RenderAsync();
        await ChooseAsync(cut, "Sort and view", "Oldest first");
        _module.SetupVoid("scrollToTop", _ => true).SetException(new Microsoft.JSInterop.JSException("gone"));
        var other = Library.Create("Comics", "#2E7D32", "Star", LibraryBookType.Digital, Guid.CreateVersion7()).Value!;
        _librariesService.GetById(other.Id.ToString()).Returns(Result<Library>.Success(other));
        var otherPage = Page(0, false);
        _booksService.GetPagedByLibrary(other.Id, 1, LibraryDetailPage.PageSize, Arg.Any<BookSortOrder>(), string.Empty, Arg.Any<CancellationToken>())
            .Returns(Result<IPagedList<BookSummaryDto>>.Success(otherPage));

        cut.Render(p => p.Add(c => c.LibraryId, other.Id.ToString()));

        await cut.WaitForAssertionAsync(() => cut.Find("h1").TextContent.Should().Be("Comics"));
        Subtitle(cut).Should().Contain("Newest first");
    }

    [Theory]
    [InlineData(LibraryBookType.Physical, true)]
    [InlineData(LibraryBookType.Digital, false)]
    public async Task LibraryDetailPage_Should_OfferNewBook_ForPhysicalBooks_AndImportInMoreActions_ForDigitalOnes(LibraryBookType bookType, bool canAddABook)
    {
        var cut = await RenderAsync(bookType);

        cut.FindAll("button[aria-label='New book']").Count.Should().Be(canAddABook ? 1 : 0);
        cut.FindAll("button[aria-label='More actions']").Count.Should().Be(canAddABook ? 0 : 1);
    }

    [Fact]
    public async Task OpenAddBookAsync_Should_OfferTheThreeWaysToAddABook_ToThisLibrary()
    {
        var cut = await RenderAsync();

        await cut.Find("button[aria-label='New book']").ClickAsync(new());

        var sheet = cut.Find("[role=dialog][aria-label='Add a book']");
        sheet.QuerySelector(".add-book-sheet-target")!.TextContent.Should().Be("to Read Books");
        sheet.QuerySelector(".add-book-sheet-primary a")!.GetAttribute("href").Should().Be($"/books/add/scan?libraryId={_library.Id}");
        sheet.QuerySelectorAll("a.action-sheet-item").Select(a => a.GetAttribute("href")).Should().Equal(
            $"/books/add/manual?libraryId={_library.Id}",
            $"/books/add/form?libraryId={_library.Id}");
        sheet.TextContent.Should().Contain("When the barcode won’t scan");
    }

    [Fact]
    public async Task ChooseSortOrderAsync_Should_SaveTheSort_AndReloadTheBooksInThatOrder()
    {
        var cut = await RenderAsync();

        await ChooseAsync(cut, "Sort and view", "Oldest first");

        await _librariesService.Received(1).Update(Arg.Is<UpdateLibraryRequest>(r => r.DefaultBookSortOrder == BookSortOrder.IdAsc), Arg.Any<CancellationToken>());
        await _booksService.Received(1).GetPagedByLibrary(_library.Id, 1, LibraryDetailPage.PageSize, BookSortOrder.IdAsc, string.Empty, Arg.Any<CancellationToken>());
        Subtitle(cut).Should().StartWith("24 books · Oldest first");
        cut.FindAll("[role=dialog]").Should().BeEmpty();
    }

    [Fact]
    public async Task ChooseSortOrderAsync_Should_KeepTheSort_WhenItCannotBeSaved()
    {
        var cut = await RenderAsync();
        _librariesService.Update(Arg.Any<UpdateLibraryRequest>(), Arg.Any<CancellationToken>()).Returns(Result<Library>.Failure(LibrariesError.NotFound));

        await ChooseAsync(cut, "Sort and view", "Series & volume");

        Subtitle(cut).Should().StartWith("24 books · Newest first");
        _snackbar.Received(1).Add("Failed to save sort order", Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ChooseViewMode_Should_ShowTheList_AndCheckIt_AndRememberIt()
    {
        var cut = await RenderAsync();

        await ChooseAsync(cut, "Sort and view", "List");

        cut.FindAll(".list-item").Should().HaveCount(2);
        await cut.Find("button[aria-label='Sort and view']").ClickAsync(new());
        cut.Find("[role=dialog] [aria-pressed=true]:contains('List')").ClassList.Should().Contain("action-sheet-item-selected");
        await _ctx.DisposeComponentsAsync();
        _stateService.Load(_library.Id)!.View.Should().Be(ViewMode.List);
    }

    [Fact]
    public async Task SearchTerm_Should_ShowTheMatchingBooksAndTheirNumber_AfterAPause()
    {
        var cut = await RenderAsync();
        GivenPage(1, "hiver", Page(1, false, _hiver));

        await TypeSearchAsync(cut, "hiver");

        await cut.WaitForAssertionAsync(() => cut.Find(".library-search-results").TextContent.Should().Be("1 book matches “hiver”"));
        cut.FindAll(".cover-item").Should().HaveCount(1);
        Subtitle(cut).Should().StartWith("24 books");
    }

    [Fact]
    public async Task SearchTerm_Should_DropTheResultsOfAnOlderSearch()
    {
        var cut = await RenderAsync();
        var olderSearch = new TaskCompletionSource<Result<IPagedList<BookSummaryDto>>>();
        var olderSearchAsked = new TaskCompletionSource();
        _booksService.GetPagedByLibrary(_library.Id, 1, LibraryDetailPage.PageSize, Arg.Any<BookSortOrder>(), "h", Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                olderSearchAsked.TrySetResult();
                return olderSearch.Task;
            });
        GivenPage(1, "hiver", Page(1, false, _hiver));

        await TypeSearchAsync(cut, "h");
        await olderSearchAsked.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        await TypeSearchAsync(cut, "hiver");
        await cut.WaitForAssertionAsync(() => cut.FindAll(".cover-item").Should().HaveCount(1));
        await cut.InvokeAsync(() => olderSearch.SetResult(Result<IPagedList<BookSummaryDto>>.Success(Page(2, false, _blacksad, _hiver))));

        cut.FindAll(".cover-item").Should().HaveCount(1);
        cut.Find(".library-search-results").TextContent.Should().Be("1 book matches “hiver”");
    }

    [Fact]
    public async Task CloseSearch_Should_ClearTheSearch_AndShowAllTheBooksAgain()
    {
        var cut = await RenderAsync();
        GivenPage(1, "zzz", Page(0, false));
        await cut.Find("button[aria-label='Search']").ClickAsync(new());
        cut.Find("header").ClassList.Should().Contain("library-bar-searching");
        await TypeSearchAsync(cut, "zzz");
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("No results for “zzz”"));

        await cut.Find("button[aria-label='Close search']").ClickAsync(new());

        cut.Find("header").ClassList.Should().NotContain("library-bar-searching");
        await cut.WaitForAssertionAsync(() => cut.FindAll(".cover-item").Should().HaveCount(2));
    }

    [Fact]
    public async Task LibraryDetailPage_Should_InviteToAddABook_WhenTheLibraryIsEmpty()
    {
        _library = Library.Create("Read Books", "#5C6BC0", "Bookmark", LibraryBookType.Physical, Guid.CreateVersion7()).Value!;
        _librariesService.GetById(_library.Id.ToString()).Returns(Result<Library>.Success(_library));
        GivenPage(1, string.Empty, Page(0, false));

        var cut = _ctx.Render<LibraryDetailPage>(p => p.Add(c => c.LibraryId, _library.Id.ToString()));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("No books in this library yet"));
        Subtitle(cut).Should().StartWith("0 books");
    }

    [Fact]
    public async Task OpenBookActionsAsync_Should_OpenTheActionsNextToTheButtonOfTheBook()
    {
        var anchor = new PopupAnchor(700, 300, 744, 344, 1024, 768);
        _module.Setup<PopupAnchor?>("anchorOf", BookActionsIds.Of(_blacksad.Id)).SetResult(anchor);
        var cut = await RenderAsync();

        await cut.Find($"#{BookActionsIds.Of(_blacksad.Id)}").ClickAsync(new());

        var sheet = cut.Find("[role=dialog]");
        sheet.GetAttribute("aria-label").Should().Be("Actions for Âme rouge");
        sheet.GetAttribute("style").Should().Be(PopupPlacement.Style(anchor, alignEnd: true));
        sheet.QuerySelector(".book-actions-serie")!.TextContent.Should().Be("Blacksad · #4");
        sheet.QuerySelectorAll(".action-sheet-item").Select(i => i.TextContent.Trim())
            .Should().Equal("Edit", "Import info from the web", "Move to another library", "Select several books", "Delete");
    }

    [Fact]
    public async Task OpenBookActionsAt_Should_OpenTheActionsOfTheLongPressedBook_WithTheDigitalOnes()
    {
        var cut = await RenderAsync(LibraryBookType.Digital);

        await cut.InvokeAsync(() => cut.Instance.OpenBookActionsAt(_hiver.Id.ToString(), PopupAnchor.At(120, 200, 1024, 768)));

        var sheet = cut.Find("[role=dialog]");
        sheet.GetAttribute("aria-label").Should().Be("Actions for Hiver nucléaire");
        sheet.QuerySelectorAll(".action-sheet-item").Select(i => i.TextContent.Trim())
            .Should().Equal("Read", "Edit", "Import info from the web", "Find ISBN", "Download CBZ", "Move to another library", "Select several books", "Delete");
    }

    [Fact]
    public async Task OpenBookActionsAt_Should_IgnoreABookThatIsNotOnScreen()
    {
        var cut = await RenderAsync();

        await cut.InvokeAsync(() => cut.Instance.OpenBookActionsAt(Guid.CreateVersion7().ToString(), PopupAnchor.At(1, 1, 1024, 768)));

        cut.FindAll("[role=dialog]").Should().BeEmpty();
    }

    [Fact]
    public async Task MoveAsync_Should_MoveTheBookChosenInItsActions_AndCountOneBookLess()
    {
        var cut = await RenderAsync();
        GivenPage(1, "hiver", Page(1, false, _hiver));
        await TypeSearchAsync(cut, "hiver");
        await cut.WaitForAssertionAsync(() => cut.FindAll(".cover-item").Should().HaveCount(1));
        await cut.InvokeAsync(() => cut.Instance.OpenBookActionsAt(_hiver.Id.ToString(), PopupAnchor.At(1, 1, 1024, 768)));
        _workflow.ChooseAndMoveAsync(_hiver.Id, Arg.Any<CancellationToken>()).Returns(true);
        GivenPage(1, "hiver", Page(0, false));

        await cut.Find("[role=dialog] .action-sheet-item:contains('Move to another library')").ClickAsync(new());

        await _workflow.Received(1).ChooseAndMoveAsync(_hiver.Id, Arg.Any<CancellationToken>());
        Subtitle(cut).Should().StartWith("23 books");
        cut.FindAll("[role=dialog]").Should().BeEmpty();
    }

    [Fact]
    public async Task LoadMoreBooksAsync_Should_AddTheNextPage()
    {
        var cut = await RenderAsync();
        GivenPage(1, string.Empty, Page(3, true, _blacksad, _hiver));
        await ChooseAsync(cut, "Sort and view", "Oldest first");
        GivenPage(2, string.Empty, Page(3, false, Book("Elementa Chaos", "Sophia", 2)));
        var observed = _ctx.JSInterop.Invocations["infiniteScroll.observe"].Count;

        await cut.InvokeAsync(cut.Instance.LoadMoreBooksAsync);

        cut.FindAll(".cover-item").Should().HaveCount(3);
        // Observed again: a sentinel still on screen asks for the following page.
        _ctx.JSInterop.Invocations["infiniteScroll.observe"].Count.Should().Be(observed + 1);
    }

    [Fact]
    public async Task LoadMoreBooksAsync_Should_DropTheNextPage_WhenTheBooksWereReloadedMeanwhile()
    {
        var cut = await RenderAsync();
        GivenPage(1, string.Empty, Page(3, true, _blacksad, _hiver));
        await ChooseAsync(cut, "Sort and view", "Oldest first");
        var nextPage = new TaskCompletionSource<Result<IPagedList<BookSummaryDto>>>();
        GivenPage(2, string.Empty, nextPage.Task);

        var loadMore = cut.InvokeAsync(cut.Instance.LoadMoreBooksAsync);
        await ChooseAsync(cut, "Sort and view", "Newest first");
        nextPage.SetResult(Result<IPagedList<BookSummaryDto>>.Success(Page(3, false, Book("Elementa Chaos", "Sophia", 2))));
        await loadMore;

        cut.FindAll(".cover-item").Should().HaveCount(2);
    }

    // ── Several books (#1150) ──────────────────────────────────────────

    private static string Heading(IRenderedComponent<LibraryDetailPage> cut) => cut.Find("h1").TextContent;

    private static Task CheckAsync(IRenderedComponent<LibraryDetailPage> cut, string title) =>
        cut.Find($"button[role=checkbox][aria-label='{title}']").ClickAsync(new());

    [Fact]
    public async Task MoveCheckedBooksAsync_Should_MoveTheCheckedBooksAtOnce_ThenShowTheBooksLeft()
    {
        var cut = await RenderAsync();
        _workflow.ChooseAndMoveAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>()).Returns(2);

        await cut.Find("button[aria-label='Select books']").ClickAsync(new());
        Heading(cut).Should().Be("Choose books");
        cut.Find(".library-bar-move").HasAttribute("disabled").Should().BeTrue();
        cut.FindAll("a.cover-link").Should().BeEmpty();
        await CheckAsync(cut, "Âme rouge");
        Heading(cut).Should().Be("1 selected");
        cut.Find("button[role=checkbox][aria-label='Âme rouge']").GetAttribute("aria-checked").Should().Be("true");
        await cut.Find(".library-bar-select-all").ClickAsync(new());
        Heading(cut).Should().Be("2 selected");
        await cut.Find(".library-bar-move").ClickAsync(new());

        await _workflow.Received(1).ChooseAndMoveAsync(
            Arg.Is<IReadOnlyList<Guid>>(ids => ids.Count == 2 && ids.Contains(_blacksad.Id) && ids.Contains(_hiver.Id)), Arg.Any<CancellationToken>());
        Heading(cut).Should().Be("Read Books");
        cut.FindAll("a.cover-link").Should().HaveCount(2);
    }

    [Fact]
    public async Task MoveCheckedBooksAsync_Should_KeepTheBooksChecked_WhenNoBookWasMoved()
    {
        var cut = await RenderAsync();
        _workflow.ChooseAndMoveAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>()).Returns(0);
        await cut.Find("button[aria-label='Select books']").ClickAsync(new());
        await CheckAsync(cut, "Hiver nucléaire");

        await cut.Find(".library-bar-move").ClickAsync(new());

        Heading(cut).Should().Be("1 selected");
        await cut.InvokeAsync(cut.Instance.MoveCheckedBooksAsync);
        await _workflow.Received(2).ChooseAndMoveAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartSelecting_Should_CheckTheBookOfItsActions_AndALongPressShouldCheckOthers()
    {
        var cut = await RenderAsync();
        await ChooseAsync(cut, "Sort and view", "List");
        await cut.InvokeAsync(() => cut.Instance.OpenBookActionsAt(_hiver.Id.ToString(), PopupAnchor.At(1, 1, 1024, 768)));

        await cut.Find("[role=dialog] .action-sheet-item:contains('Select several books')").ClickAsync(new());
        cut.FindAll("[role=dialog]").Should().BeEmpty();
        Heading(cut).Should().Be("1 selected");
        cut.Find(".list-item-checked .list-title").TextContent.Should().Be("Hiver nucléaire");

        await cut.InvokeAsync(() => cut.Instance.OpenBookActionsAt(_blacksad.Id.ToString(), PopupAnchor.At(1, 1, 1024, 768)));
        Heading(cut).Should().Be("2 selected");
        await cut.InvokeAsync(() => cut.Instance.OpenBookActionsAt(_blacksad.Id.ToString(), PopupAnchor.At(1, 1, 1024, 768)));
        Heading(cut).Should().Be("1 selected");
        cut.FindAll("[role=dialog]").Should().BeEmpty();

        await cut.Find("button[aria-label='Stop choosing books']").ClickAsync(new());
        Heading(cut).Should().Be("Read Books");
        cut.FindAll("button[role=checkbox]").Should().BeEmpty();
    }

    [Fact]
    public async Task StartSelecting_Should_KeepTheStopButtonShown_WhenTheScreenIsLarge()
    {
        // The back button is hidden from 1440px on (#1172): the stop of the selection must not share its class.
        var cut = await RenderAsync();

        await cut.Find("button[aria-label='Select books']").ClickAsync(new());

        var stop = cut.Find("button[aria-label='Stop choosing books']");
        stop.ClassList.Should().Contain("library-bar-close").And.NotContain("library-bar-back");
    }
}

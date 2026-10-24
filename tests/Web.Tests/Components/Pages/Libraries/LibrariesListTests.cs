using Application.Books.Search;
using Application.Libraries.Summaries;
using AwesomeAssertions;
using Bunit;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages.Dialogs;
using Web.Components.Pages.Libraries;
using Web.Infrastructure;
using Web.Models;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Libraries;

public sealed class LibrariesListTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly ILibrariesService _librariesService = Substitute.For<ILibrariesService>();
    private readonly IBooksService _booksService = Substitute.For<IBooksService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly BunitJSModuleInterop _module;

    private readonly LibrarySummary _readBooks = new(Guid.CreateVersion7(), "Read Books", "#2E7D32", "Star", LibraryBookType.Physical, 24)
    {
        Covers = ["/covers/3.webp", "/covers/2.webp", "/covers/1.webp"]
    };
    private readonly LibrarySummary _toSort = new(Guid.CreateVersion7(), "À trier", "#4A5BC4", "Bookmark", LibraryBookType.Digital, 2);
    private readonly LibrarySummary _ebooks = new(Guid.CreateVersion7(), "MesEbooks", "#4A5BC4", "Bookmark", LibraryBookType.Digital, 0);

    public LibrariesListTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _module = _ctx.JSInterop.SetupModule(AppVersion.Versioned("./js/popupAnchor.js"));
        _module.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_librariesService);
        _ctx.Services.AddSingleton(_booksService);
        _ctx.Services.AddSingleton(_dialogService);
        _ctx.Services.AddSingleton(_snackbar);
        _ctx.Services.AddScoped<PopupAnchorLocator>();
    }

    public async ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        await _ctx.DisposeAsync();
    }

    private void GivenLibraries(params LibrarySummary[] libraries) =>
        _librariesService.GetSummaries(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<LibrarySummary>>.Success(libraries));

    private void GivenConfirmation(bool confirmed)
    {
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(confirmed ? DialogResult.Ok(true) : DialogResult.Cancel());
        _dialogService.ShowAsync<ConfirmationDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>()).Returns(dialog);
    }

    private IRenderedComponent<LibrariesList> Render(params LibrarySummary[] libraries)
    {
        GivenLibraries(libraries);
        return _ctx.Render<LibrariesList>();
    }

    private static async Task OpenActionsAsync(IRenderedComponent<LibrariesList> cut, LibrarySummary library) =>
        await cut.Find($"#library-actions-{library.Id}").ClickAsync(new());

    [Fact]
    public void Render_Should_ShowACardPerLibrary_WithTheTotalsInTheSubtitle()
    {
        var cut = Render(_readBooks, _toSort, _ebooks);

        cut.FindAll(".library-name").Select(n => n.TextContent).Should().Equal("Read Books", "À trier", "MesEbooks");
        cut.FindAll(".library-type").Select(n => n.TextContent).Should().Equal("Physical", "Digital", "Digital");
        cut.FindAll(".library-count").Select(n => n.TextContent).Should().Equal("24 books", "2 books", "No books yet");
        cut.Find(".libraries-bar-subtitle").TextContent.Should().Be("3 libraries · 26 books");
        cut.FindAll(".library-card-link").Select(l => l.GetAttribute("href")).Should().Equal(
            $"/libraries/{_readBooks.Id}", $"/libraries/{_toSort.Id}", $"/libraries/{_ebooks.Id}");
        cut.Find($"#library-actions-{_readBooks.Id}").GetAttribute("aria-label").Should().Be("More actions for Read Books");
    }

    [Fact]
    public void Render_Should_FanTheLatestCoversWithTheNewestInFront()
    {
        var cut = Render(_readBooks);

        // The newest cover is drawn last so that it sits on top of the others.
        cut.FindAll(".library-covers img").Select(i => (i.GetAttribute("src"), i.GetAttribute("class")))
            .Should().Equal(("/covers/1.webp", "library-cover library-cover-2"), ("/covers/2.webp", "library-cover library-cover-1"), ("/covers/3.webp", "library-cover library-cover-0"));
    }

    [Fact]
    public void Render_Should_ShowADashedSlotForAnEmptyLibrary_AndTheIconForBooksWithoutCovers()
    {
        var cut = Render(_toSort, _ebooks);

        var covers = cut.FindAll(".library-covers");
        covers[0].QuerySelector(".library-cover-none").Should().NotBeNull();
        covers[1].QuerySelector(".library-cover-empty").Should().NotBeNull();
        cut.FindAll(".library-covers img").Should().BeEmpty();
    }

    private void GivenSearch(string term, int bookLimit, Result<BookSearchResult> result) =>
        _booksService.Search(term, bookLimit, Arg.Any<CancellationToken>()).Returns(result);

    private static void Type(IRenderedComponent<LibrariesList> cut, string term) =>
        cut.Find(".libraries-search-field input").Input(term);

    private BookSearchHit Hit(string title, int volume) =>
        new(Guid.CreateVersion7(), _readBooks.Id, title, "Astérix", volume, $"/covers/{volume}.webp");

    [Fact]
    public void SearchTerm_Should_ShowTheSeriesThenTheBooksOfAllLibraries_InPlaceOfTheCards()
    {
        var gaulois = Hit("Astérix le Gaulois", 1);
        GivenSearch("astérix", LibrariesList.SearchPageSize, new BookSearchResult(
            [new SerieSearchHit(_readBooks.Id, "Astérix", 2), new SerieSearchHit(_toSort.Id, "Astérix", 1)],
            [gaulois, new BookSearchHit(Guid.CreateVersion7(), _toSort.Id, "La Serpe d'or", "Astérix", 2, "")],
            2));
        var cut = Render(_readBooks, _toSort);

        Type(cut, " astérix ");

        cut.WaitForAssertion(() => cut.FindAll(".search-heading").Select(h => h.TextContent).Should().Equal("Series · 2", "Books · 2"));
        cut.FindAll(".library-card").Should().BeEmpty();
        cut.FindAll(".search-row .search-chip").Select(c => c.TextContent).Should().Equal("Read Books", "À trier", "Read Books", "À trier");
        cut.FindAll(".search-row").Take(3).Select(r => r.GetAttribute("href")).Should().Equal(
            $"/libraries/{_readBooks.Id}/serie?name=Ast%C3%A9rix", $"/libraries/{_toSort.Id}/serie?name=Ast%C3%A9rix", $"/books/{gaulois.Id}");
        cut.FindAll("img.search-cover").Should().ContainSingle();
        cut.FindAll(".search-cover-none").Should().ContainSingle();
        cut.FindAll(".search-more").Should().BeEmpty();
    }

    [Fact]
    public async Task ShowMoreAsync_Should_ShowMoreBooks_WhenThereAreMoreThanAPage()
    {
        var page = Enumerable.Range(1, LibrariesList.SearchPageSize).Select(i => Hit($"Book {i}", i)).ToList();
        GivenSearch("astérix", LibrariesList.SearchPageSize, new BookSearchResult([], page, 21));
        GivenSearch("astérix", LibrariesList.SearchPageSize * 2, new BookSearchResult([], [.. page, Hit("Book 21", 21)], 21));
        var cut = Render(_readBooks);
        Type(cut, "astérix");
        cut.WaitForAssertion(() => cut.Find(".search-more").TextContent.Should().Be("Show 1 more book"));

        await cut.Find(".search-more").ClickAsync(new());

        cut.WaitForAssertion(() => cut.FindAll(".search-row").Should().HaveCount(21));
        cut.FindAll(".search-more").Should().BeEmpty();
    }

    [Fact]
    public async Task SearchTerm_Should_SayNothingWasFound_AndClearTheSearchToShowTheCardsAgain()
    {
        GivenSearch("zorglub", LibrariesList.SearchPageSize, BookSearchResult.Empty);
        var cut = Render(_readBooks);
        Type(cut, "zorglub");
        cut.WaitForAssertion(() => cut.Find(".search-results").TextContent.Should().Contain("No results for “zorglub”"));

        await cut.FindAll(".search-results button").Single(b => b.TextContent.Contains("Clear search", StringComparison.Ordinal)).ClickAsync(new());

        cut.WaitForAssertion(() => cut.FindAll(".search-results").Should().BeEmpty());
        cut.FindAll(".library-card").Should().ContainSingle();
        cut.Find(".libraries-search-field input").GetAttribute("value").Should().BeNullOrEmpty();
    }

    [Fact]
    public void SearchTerm_Should_ReportTheError_WhenTheSearchFails()
    {
        GivenSearch("astérix", LibrariesList.SearchPageSize, Result<BookSearchResult>.Failure(BooksError.BadRequest));
        var cut = Render(_readBooks);

        Type(cut, "astérix");

        cut.WaitForAssertion(() => _snackbar.Received(1).Add(LibrariesList.Msg_SearchFailed, Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>()));
        cut.FindAll(".search-row").Should().BeEmpty();
    }

    [Fact]
    public void SearchTerm_Should_DropTheSearch_WhenTheFieldIsEmptiedBeforeThePause()
    {
        var cut = Render(_readBooks);

        Type(cut, "ast");
        Type(cut, "  ");

        cut.FindAll(".search-results").Should().BeEmpty();
        cut.FindAll(".library-card").Should().ContainSingle();
        _booksService.DidNotReceive().Search(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Render_Should_InviteToCreateALibrary_WhenThereIsNone()
    {
        var cut = Render();

        cut.Find(".libraries-empty").TextContent.Should().Contain("No libraries yet");
        cut.FindAll(".libraries-bar-subtitle").Should().BeEmpty();
        cut.FindAll("a[href='/libraries/add']").Should().HaveCount(3);
    }

    [Fact]
    public void Render_Should_ReportTheError_WhenTheLibrariesCannotBeLoaded()
    {
        _librariesService.GetSummaries(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<LibrarySummary>>.Failure(LibrariesError.ValidationError));

        var cut = _ctx.Render<LibrariesList>();

        _snackbar.Received(1).Add(LibrariesList.Msg_LoadFailed, Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        cut.FindAll(".library-card").Should().BeEmpty();
    }

    [Fact]
    public async Task OpenActionsAsync_Should_ShowTheActionsOfAPhysicalLibrary_NextToItsButton()
    {
        var anchor = new PopupAnchor(700, 300, 744, 344, 1280, 800);
        _module.Setup<PopupAnchor?>("anchorOf", $"library-actions-{_readBooks.Id}").SetResult(anchor);
        var cut = Render(_readBooks, _toSort);

        await OpenActionsAsync(cut, _readBooks);

        var sheet = cut.Find("[role=dialog]");
        sheet.GetAttribute("aria-label").Should().Be("Actions for Read Books");
        sheet.GetAttribute("style").Should().Be(PopupPlacement.Style(anchor, alignEnd: true));
        sheet.QuerySelector(".library-actions-details")!.TextContent.Should().Be("Physical · 24 books");
        sheet.QuerySelectorAll(".action-sheet-item").Select(i => i.TextContent.Trim())
            .Should().Equal("Edit name, icon and color", "Delete library");
        sheet.QuerySelector("a.action-sheet-item")!.GetAttribute("href").Should().Be($"/libraries/{_readBooks.Id}/edit");
    }

    [Fact]
    public async Task OpenActionsAsync_Should_OfferTheImport_ForADigitalLibrary()
    {
        var cut = Render(_readBooks, _toSort);

        await OpenActionsAsync(cut, _toSort);

        var sheet = cut.Find("[role=dialog]");
        sheet.QuerySelectorAll(".action-sheet-item").Select(i => i.TextContent.Trim())
            .Should().Equal("Edit name, icon and color", "Import books", "Delete library");
        sheet.QuerySelector($"a[href='/import?libraryId={_toSort.Id}']").Should().NotBeNull();
    }

    [Fact]
    public async Task OpenActionsAsync_Should_NotOfferToDeleteTheImportedLibrary()
    {
        var imported = new LibrarySummary(Guid.CreateVersion7(), "Imported", "#4A5BC4", "Inbox", LibraryBookType.Digital, 3);
        var cut = Render(_readBooks, imported);

        await OpenActionsAsync(cut, imported);

        cut.Find("[role=dialog]").QuerySelectorAll(".action-sheet-item").Select(i => i.TextContent.Trim())
            .Should().Equal("Edit name, icon and color", "Import books");
    }

    [Fact]
    public async Task OpenActionsAsync_Should_CenterTheMenu_WhenTheButtonCannotBeLocated()
    {
        _module.Setup<PopupAnchor?>("anchorOf", $"library-actions-{_readBooks.Id}").SetException(new Microsoft.JSInterop.JSException("gone"));
        var cut = Render(_readBooks);

        await OpenActionsAsync(cut, _readBooks);

        cut.Find("[role=dialog]").GetAttribute("style").Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task CloseActions_Should_HideTheActions()
    {
        var cut = Render(_readBooks);
        await OpenActionsAsync(cut, _readBooks);

        await cut.Find(".action-sheet-backdrop").ClickAsync(new());

        cut.FindAll("[role=dialog]").Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_Should_DeleteTheLibraryAndReload_WhenTheUserConfirms()
    {
        GivenConfirmation(confirmed: true);
        _librariesService.Delete(_toSort.Id.ToString(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        var cut = Render(_readBooks, _toSort);
        await OpenActionsAsync(cut, _toSort);
        GivenLibraries(_readBooks);

        await cut.FindAll(".action-sheet-item")[^1].ClickAsync(new());

        await _dialogService.Received(1).ShowAsync<ConfirmationDialog>("Delete library", Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>());
        _snackbar.Received(1).Add(LibrariesList.Msg_LibCorrectlyDeleted, Severity.Success, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        cut.WaitForAssertion(() => cut.FindAll(".library-name").Select(n => n.TextContent).Should().Equal("Read Books"));
        cut.FindAll("[role=dialog]").Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_Should_KeepTheLibrary_WhenTheUserCancels()
    {
        GivenConfirmation(confirmed: false);
        var cut = Render(_readBooks);
        await OpenActionsAsync(cut, _readBooks);

        await cut.FindAll(".action-sheet-item")[^1].ClickAsync(new());

        await _librariesService.DidNotReceive().Delete(Arg.Any<string?>(), Arg.Any<CancellationToken>());
        cut.FindAll(".library-card").Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteAsync_Should_ReportTheError_WhenTheLibraryCannotBeDeleted()
    {
        GivenConfirmation(confirmed: true);
        _librariesService.Delete(_readBooks.Id.ToString(), Arg.Any<CancellationToken>()).Returns(Result.Failure(LibrariesError.FolderNotDeleted));
        var cut = Render(_readBooks);
        await OpenActionsAsync(cut, _readBooks);

        await cut.FindAll(".action-sheet-item")[^1].ClickAsync(new());

        _snackbar.Received(1).Add(LibrariesList.Msg_DeleteFailed, Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        cut.FindAll(".library-card").Should().HaveCount(1);
    }
}
